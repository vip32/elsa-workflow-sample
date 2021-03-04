using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Http;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Primitives;
using Elsa.Activities.Temporal;
using Elsa.Builders;
using Elsa.Serialization;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Net;

namespace Presentation.Web.Server
{
    /// <summary>
    /// Demonstrates the Fork, Join, Timer and Signal activities working together to model a long-running process where either the timer causes the workflow to resume, or a signal.
    /// </summary>
    public class DemoHttpWorkflow : IWorkflow
    {
        private readonly IClock _clock;
        private readonly Duration _timeOut;

        public DemoHttpWorkflow(IClock clock)
        {
            _clock = clock;
            _timeOut = Duration.FromSeconds(10);
        }

        public void Build(IWorkflowBuilder builder)
        {
            var serializer = builder.ServiceProvider.GetRequiredService<IContentSerializer>();

            builder
                // The workflow context type of this workflow
                .WithContextType<DemoHttpWorkflowContext>()

                // Accept HTTP requests to submit new orders
                .HttpEndpoint(activity => activity.WithPath("/_workflows/demo").WithMethod(HttpMethods.Post).WithTargetType<Order>()).WithName("HttpRequest")

                // Store the order in the workflow context. It will be saved automatically when the workflow gets suspended
                .Then(context => context.SetWorkflowContext(
                    new DemoHttpWorkflowContext
                    {
                        CorrelationId = Guid.NewGuid().ToString("N"),
                        Order = context.GetOutputFrom<HttpRequestModel>("HttpRequest").GetBody<Order>(),
                        Comments = new List<Comment>()
                    })).LoadWorkflowContext()

                // Correlate the workflow.
                .Correlate(context => ((DemoHttpWorkflowContext)context.WorkflowExecutionContext.WorkflowContext!).CorrelationId)
                .WriteLine(context => $"CorrelationId={GetCorrelationId(context)}")

                // Log then new order
                .WriteLine(context => $"Received order for {GetOrder(context).Name} ({GetOrder(context).Email})")
                .WriteLine(context => $"Received order details: {JsonConvert.SerializeObject(GetOrder(context))}")

                // Save some variables
                .SetVariable("OrderId", context => GetOrder(context).Id)
                .SetVariable("Status", DemoHttpWorkflowStatus.New)

                .WriteHttpResponse(activity => activity
                    .WithStatusCode(HttpStatusCode.Accepted)
                    .WithContentType("application/json")
                    .WithResponseHeaders(context => new HttpResponseHeaders() { ["CorrelationId"] = context.CorrelationId })
                    .WithContent(context =>
                    {
                        var request = context.GetOutputFrom<HttpRequestModel>("HttpRequest");
                        var model = request.GetBody<Order>();
                        return serializer.Serialize(model);
                    }))

                .WriteLine(context => $"The demo completes in {_timeOut.ToString()} ({_clock.GetCurrentInstant().Plus(_timeOut)}). Can't wait that long? Send me the secret \"hurry\" signal! (http://localhost:7304/signal/hurry/trigger?correlationId={GetCorrelationId(context)})")
                .Then<Fork>(
                    fork => fork.WithBranches("Approve", "Reject", "Timer"),
                    fork =>
                    {
                        var approveBranch = fork
                            .When("Approve")
                            .ReceiveHttpPostRequest<Comment>(context => $"/_workflows/demo/approve") // ?correlation=GUID
                            .Then(StoreComment)
                            //.Then(ThrowErrorIfFinished)
                            .If(
                                context => context.GetVariable<bool>("IsProcessed"),
                                whenTrue => whenTrue.WriteLine("++++++++++++ STOP"), //throw new Exception("Something bad happened. Please retry workflow."),
                                whenFalse => whenFalse
                                    .SetVariable("IsProcessed", true)
                                    .WriteLine("++++++++++++ CONTINUE2"))
                            .WriteLine(context => $"APPROVED: {GetOrder(context).Id}");

                        var rejectBranch = fork
                            .When("Reject")
                            .ReceiveHttpPostRequest<Comment>(context => $"/_workflows/demo/reject") // ?correlation=GUID
                            .Then(StoreComment)
                            //.Then(ThrowErrorIfFinished)
                            .If(
                                context => context.GetVariable<bool>("IsProcessed"),
                                whenTrue => whenTrue.WriteLine("++++++++++++ STOP"), //throw new Exception("Something bad happened. Please retry workflow."),
                                whenFalse => whenFalse
                                    .SetVariable("IsProcessed", true)
                                    .WriteLine("++++++++++++ CONTINUE2"))
                            .WriteLine(context => $"REJECTED: {GetOrder(context).Id}");

                        fork
                            .When("Timer")
                            .Timer(_timeOut)
                            .SetVariable("RejectedBy", "Timer")
                            .Then(StoreApproveTimeoutComment)
                            .WriteLine(context => $"WORKFLOW {GetCorrelationId(context)}  {GetOrder(context).Id} TIMEOUT--")
                            .Then("Join");

                        WriteApproveResponse(approveBranch, order => $"Thanks for approving document {order.Id}!")
                            .WriteLine(context => $"WORKFLOW {GetCorrelationId(context)} {GetOrder(context).Id} APPROVED++ (UserId={context.GetVariable<string>("UserId")})")
                            .SetVariable("ApprovedBy", "User")
                            .Then("Join");
                        //.Then(join);
                        WriteApproveResponse(rejectBranch, order => $"Thanks for rejecting document {order.Id}!")
                            .WriteLine(context => $"WORKFLOW {GetCorrelationId(context)}  {GetOrder(context).Id} REJECTED-- (UserId={context.GetVariable<string>("UserId")})")
                            .SetVariable("ApprovedBy", "User")
                            .Then("Join");
                        //.Then(join);
                    })
                .Add<Join>(x => x.WithMode(Join.JoinMode.WaitAny)).WithName("Join")
                //.Then<Fork>(
                //    fork => fork.WithBranches("Timer", "Signal"),
                //    fork =>
                //    {
                //        fork
                //            .When("Timer")
                //            .Timer(_timeOut)
                //            .SetVariable("CompletedVia", "Timer")
                //            .Then("Join");

                //        fork
                //            .When("Signal")
                //            .SignalReceived("hurry")
                //            .SetVariable("CompletedVia", "Signal")
                //            .Then("Join");
                //    })
                //.Add<Join>(x => x.WithMode(Join.JoinMode.WaitAny)).WithName("Join")
                .WriteLine(context => $"Finished order for {GetOrder(context).Name} ({GetOrder(context).Email})")
                .WriteLine(context => $"Demo {GetCorrelationId(context)} (OrderId={context.GetVariable<string>("OrderId")}) completed successfully via {context.GetVariable<string>("CompletedVia")}!");
        }

        private string GetCorrelationId(ActivityExecutionContext context) => context.WorkflowExecutionContext.CorrelationId;

        private static Order GetOrder(ActivityExecutionContext context) => context.GetWorkflowContext<DemoHttpWorkflowContext>().Order;

        private static void StoreComment(ActivityExecutionContext context)
        {
            var workflowContext = (DemoHttpWorkflowContext)context.WorkflowExecutionContext.WorkflowContext!;
            var comment = (Comment)((HttpRequestModel)context.Input!).Body!;
            workflowContext.Comments.Add(comment);
        }

        private static void StoreApproveTimeoutComment(ActivityExecutionContext context)
        {
            var workflowContext = (DemoHttpWorkflowContext)context.WorkflowExecutionContext.WorkflowContext!;
            var comment = new Comment { Text = "timout", Author = "timer" };
            workflowContext.Comments.Add(comment);
        }

        private static IActivityBuilder WriteApproveResponse(IBuilder builder, Func<Order, string> html) =>
           builder
               .WriteHttpResponse(
                   activity => activity
                       .WithStatusCode(HttpStatusCode.OK)
                       .WithContentType("text/html")
                       .WithResponseHeaders(context => new HttpResponseHeaders() { ["CorrelationId"] = context.CorrelationId })
                       .WithContent(
                           context =>
                           {
                               var model = GetOrder(context);
                               return html(model);
                           }));
    }

    public enum DemoHttpWorkflowStatus
    {
        New,
        Checked,
        Delivered
    }
}