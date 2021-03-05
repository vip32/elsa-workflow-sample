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
        private readonly IClock clock;
        private readonly Duration timeOut;

        public DemoHttpWorkflow(IClock clock)
        {
            this.clock = clock;
            this.timeOut = Duration.FromSeconds(10);
        }

        public void Build(IWorkflowBuilder builder)
        {
            //var serializer = builder.ServiceProvider.GetRequiredService<IContentSerializer>();

            builder
                // The workflow context type of this workflow
                .WithContextType<WorkflowState>()

                // Accept HTTP requests to submit new orders
                //.HttpEndpoint(activity => activity
                //    .WithPath("/_workflows/demo")
                //    .WithMethod(HttpMethods.Post)
                //    .WithTargetType<Order>()).WithName("HttpRequest")

                // Store the order in the wokflow state. It will be saved automatically when the workflow gets suspended
                .Then(context => context.SetWorkflowContext(
                    new WorkflowState
                    {
                        CorrelationId = context.CorrelationId, // ((WorkflowState)context.WorkflowExecutionContext.WorkflowContext!).CorrelationId, //Guid.NewGuid().ToString("N"),
                        Order = context.GetInput<Order>() //context.GetOutputFrom<HttpRequestModel>("HttpRequest").GetBody<Order>()
                    })).LoadWorkflowContext()

                // Correlate the workflow.
                .Correlate(context => ((WorkflowState)context.WorkflowExecutionContext.WorkflowContext!).CorrelationId)

                // Log then new order
                .WriteLine(context => $"CorrelationId={this.GetCorrelationId(context)}")
                .WriteLine(context => $"Received order for {GetOrder(context).Name} ({GetOrder(context).Email})")
                .WriteLine(context => $"Received order details: {JsonConvert.SerializeObject(GetOrder(context))}")

                // Save some variables
                .SetVariable("OrderId", context => GetOrder(context).Id) // not needed > state
                .SetVariable("Status", DemoHttpWorkflowStatus.New) // not needed > state

                //.WriteHttpResponse(activity => activity
                //    .WithStatusCode(HttpStatusCode.Accepted)
                //    .WithContentType("application/json")
                //    .WithResponseHeaders(context => new HttpResponseHeaders() { ["CorrelationId"] = context.CorrelationId })
                //    .WithContent(context =>
                //    {
                //        //var request = context.GetOutputFrom<HttpRequestModel>("HttpRequest");
                //        //var model = request.GetBody<Order>();
                //        //return serializer.Serialize(model);
                //        return serializer.Serialize(GetState(context));
                //    }))

                .WriteLine(context => $"The demo completes in {this.timeOut.ToString()} ({this.clock.GetCurrentInstant().Plus(this.timeOut)}). Can't wait that long? Send me the secret \"hurry\" signal! (http://localhost:7304/signal/hurry/trigger?correlationId={this.GetCorrelationId(context)})")
                .Then<Fork>(
                    fork => fork.WithBranches("Approve", "Reject", "Timer"),
                    fork =>
                    {
                        fork
                            .When("Approve")
                            //.ReceiveHttpPostRequest<Comment>(context => $"/_workflows/demo/approve") // ?correlation=GUID
                            .SignalReceived("approve")
                            .WriteLine(context => $"WORKFLOW {this.GetCorrelationId(context)} {GetOrder(context).Id} APPROVED++ (UserId={context.GetVariable<string>("UserId")})")
                            .Then(StoreComment)
                            .Then(context => StoreStatus(context, DemoHttpWorkflowStatus.Approved))
                            //.Then(ThrowErrorIfFinished)
                            .If(
                                context => context.GetVariable<bool>("IsProcessed"),
                                whenTrue => whenTrue.WriteLine("++++++++++++ STOP"), //throw new Exception("Something bad happened. Please retry workflow."),
                                whenFalse => whenFalse
                                    .SetVariable("IsProcessed", true)
                                    .WriteLine("++++++++++++ CONTINUE2"))
                            .WriteLine(context => $"APPROVED: {GetOrder(context).Id}")
                            .Then("Join");

                        fork
                            .When("Reject")
                            //.ReceiveHttpPostRequest<Comment>(context => $"/_workflows/demo/reject") // ?correlation=GUID
                            .SignalReceived("reject")
                            .WriteLine(context => $"WORKFLOW {this.GetCorrelationId(context)}  {GetOrder(context).Id} REJECTED-- (UserId={context.GetVariable<string>("UserId")})")
                            .Then(StoreComment)
                            .Then(context => StoreStatus(context, DemoHttpWorkflowStatus.Rejected))
                            //.Then(ThrowErrorIfFinished)
                            .If(
                                context => context.GetVariable<bool>("IsProcessed"),
                                whenTrue => whenTrue.WriteLine("++++++++++++ STOP"), //throw new Exception("Something bad happened. Please retry workflow."),
                                whenFalse => whenFalse
                                    .SetVariable("IsProcessed", true)
                                    .WriteLine("++++++++++++ CONTINUE2"))
                            .WriteLine(context => $"REJECTED: {GetOrder(context).Id}")
                            .Then("Join");

                        fork
                            .When("Timer")
                            .Timer(this.timeOut)
                            .WriteLine(context => $"WORKFLOW {this.GetCorrelationId(context)}  {GetOrder(context).Id} TIMEOUT--")
                            .SetVariable("RejectedBy", "Timer")
                            .Then(StoreApproveTimeoutComment)
                            .Then(context => StoreStatus(context, DemoHttpWorkflowStatus.Rejected))
                            .Then("Join");
                    })
                .Add<Join>(x => x.WithMode(Join.JoinMode.WaitAny)).WithName("Join")
                .WriteLine(context => $"Finished order for {GetOrder(context).Name} ({GetOrder(context).Email})")
                .WriteLine(context => $"Demo {this.GetCorrelationId(context)} (OrderId={context.GetVariable<string>("OrderId")}) completed successfully via {context.GetVariable<string>("CompletedVia")}!");
        }

        private string GetCorrelationId(ActivityExecutionContext context) => context.WorkflowExecutionContext.CorrelationId;

        private static WorkflowState GetState(ActivityExecutionContext context) => context.GetWorkflowContext<WorkflowState>();

        private static Order GetOrder(ActivityExecutionContext context) => GetState(context).Order;

        private static ICollection<Comment> GetComments(ActivityExecutionContext context) => GetState(context).Comments;

        private static void StoreStatus(ActivityExecutionContext context, DemoHttpWorkflowStatus status)
        {
            var state = (WorkflowState)context.WorkflowExecutionContext.WorkflowContext!;
            state.Status = status;
        }

        private static void StoreComment(ActivityExecutionContext context)
        {
            var state = (WorkflowState)context.WorkflowExecutionContext.WorkflowContext!;
            var comment = context.GetInput<Comment>();//(Comment)((HttpRequestModel)context.Input!).Body!;
            state.Comments.Add(comment);
        }

        private static void StoreApproveTimeoutComment(ActivityExecutionContext context)
        {
            var state = (WorkflowState)context.WorkflowExecutionContext.WorkflowContext!;
            var comment = new Comment { Text = "timout", Author = "timer" };
            state.Comments.Add(comment);
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
        Approved,
        Rejected,
        Processed,
        Shipped,
        Delivered
    }
}