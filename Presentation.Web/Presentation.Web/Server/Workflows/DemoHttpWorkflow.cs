namespace Presentation.Web.Server
{
    using Elsa.Activities.ControlFlow;
    using Elsa.Activities.Primitives;
    using Elsa.Activities.Temporal;
    using Elsa.Builders;
    using Elsa.Services.Models;
    using Newtonsoft.Json;
    using NodaTime;
    using System.Collections.Generic;

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
            builder
                // The workflow context type of this workflow
                .WithContextType<WorkflowState>()

                // Store the order in the wokflow state. It will be saved automatically when the workflow gets suspended
                .Then(context => context.SetWorkflowContext(
                    new WorkflowState
                    {
                        CorrelationId = context.CorrelationId,
                        Order = context.GetInput<Order>()
                    })).LoadWorkflowContext()

                // Correlate the workflow.
                .Correlate(context => ((WorkflowState)context.WorkflowExecutionContext.WorkflowContext!).CorrelationId)

                // Log then new order
                .LogInformation(context => $"order received (correlationId={this.GetCorrelationId(context)}, name={GetOrder(context).Name}, email={GetOrder(context).Email})")
                //.LogInformation(context => $"order details: {JsonConvert.SerializeObject(GetOrder(context))}")
                .LogInformation(context => $"order approval completes in {this.timeOut} ({this.clock.GetCurrentInstant().Plus(this.timeOut)}). Can't wait that long? Send me the secret \"hurry\" signal! (https://localhost:5001/_workflows/demo/approve?correlationId={this.GetCorrelationId(context)})")

                .Then<Fork>(
                    fork => fork.WithBranches("Approve", "Reject", "Timer"),
                    fork =>
                    {
                        fork
                            .When("Approve")
                            .SignalReceived("approve")
                            .LogInformation(context => $"WORKFLOW {this.GetCorrelationId(context)} {GetOrder(context).Id} APPROVED++ (UserId={context.GetVariable<string>("UserId")})")
                            .Then(context => StoreStatus(context, DemoHttpWorkflowStatus.Approved))
                            .Then(StoreComment)
                            //.Then(ThrowErrorIfFinished)
                            .If(
                                context => context.GetVariable<bool>("IsProcessed"),
                                whenTrue => whenTrue.LogInformation("++++++++++++ STOP"), //throw new Exception("Something bad happened. Please retry workflow."),
                                whenFalse => whenFalse
                                    .SetVariable("IsProcessed", true)
                                    .LogInformation("++++++++++++ CONTINUE2"))
                            .LogInformation(context => $"APPROVED: {GetOrder(context).Id}")
                            .Then("Join");

                        fork
                            .When("Reject")
                            .SignalReceived("reject")
                            .LogInformation(context => $"WORKFLOW {this.GetCorrelationId(context)} {GetOrder(context).Id} REJECTED-- (UserId={context.GetVariable<string>("UserId")})")
                            .Then(context => StoreStatus(context, DemoHttpWorkflowStatus.Rejected))
                            .Then(StoreComment)
                            //.Then(ThrowErrorIfFinished)
                            .If(
                                context => context.GetVariable<bool>("IsProcessed"),
                                whenTrue => whenTrue.LogInformation("++++++++++++ STOP"), //throw new Exception("Something bad happened. Please retry workflow."),
                                whenFalse => whenFalse
                                    .SetVariable("IsProcessed", true)
                                    .LogInformation("++++++++++++ CONTINUE2"))
                            .LogInformation(context => $"REJECTED: {GetOrder(context).Id}")
                            .Then("Join");

                        fork
                            .When("Timer")
                            .Timer(this.timeOut)
                            .LogInformation(context => $"WORKFLOW {this.GetCorrelationId(context)}  {GetOrder(context).Id} TIMEOUT--")
                            .Then(context => StoreStatus(context, DemoHttpWorkflowStatus.Rejected))
                            .Then(StoreApproveTimeoutComment)
                            .SetVariable("RejectedBy", "Timer")
                            .Then("Join");
                    })
                .Add<Join>(x => x.WithMode(Join.JoinMode.WaitAny)).WithName("Join")
                .LogInformation(context => $"Finished order for {GetOrder(context).Name} ({GetOrder(context).Email}, Status={GetState(context).Status})")
                .LogInformation(context => $"Demo {this.GetCorrelationId(context)} (OrderId={context.GetVariable<string>("OrderId")}) completed successfully via {context.GetVariable<string>("CompletedVia")}!")
                .Finish();
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
    }
}