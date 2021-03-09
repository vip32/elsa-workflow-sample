namespace Presentation.Web.Server
{
    using Elsa.Activities.ControlFlow;
    using Elsa.Activities.Primitives;
    using Elsa.Activities.Temporal;
    using Elsa.Builders;
    using Elsa.Services.Models;
    using Newtonsoft.Json;
    using NodaTime;
    using System;
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
                .LogInformation(context => $"order received (name={GetOrder(context).Name}, email={GetOrder(context).Email})")
                //.LogInformation(context => $"order details: {JsonConvert.SerializeObject(GetOrder(context))}")
                .LogInformation(context => $"order approval completes in {this.timeOut} ({this.clock.GetCurrentInstant().Plus(this.timeOut)}). Can't wait that long? Send me the secret \"hurry\" signal! (https://localhost:5001/_workflows/demo/approve?correlationId={this.GetCorrelationId(context)})")

                .Then<Fork>(
                    fork => fork.WithBranches("Approve", "Reject", "Timer"),
                    fork =>
                    {
                        fork
                            .When("Approve").SetName("Approve01")
                            .SignalReceived("approve")
                            .Then(ThrowErrorWhenStatusNotNew)
                            .LogInformation(context => $"WORKFLOW {GetOrder(context).Id} APPROVED01++ (UserId={context.GetVariable<string>("UserId")})")
                            .Then(context => StoreStatus(context, DemoHttpWorkflowStatus.Approved))
                            .Then(StoreComment)
                            //.Then(ThrowErrorIfFinished)
                            .LogInformation(context => $"APPROVED: {GetOrder(context).Id}")
                            .Then("Join");

                        fork
                            .When("Reject").SetName("Reject02")
                            .SignalReceived("reject")
                            .Then(ThrowErrorWhenStatusNotNew)
                            .LogInformation(context => $"WORKFLOW {GetOrder(context).Id} REJECTED02-- (UserId={context.GetVariable<string>("UserId")})")
                            .Then(context => StoreStatus(context, DemoHttpWorkflowStatus.Rejected))
                            .Then(StoreComment)
                            //.Then(ThrowErrorIfFinished)
                            .LogInformation(context => $"REJECTED: {GetOrder(context).Id}")
                            .Then("Join");

                        //fork
                        //    .When("Timer")
                        //    .Timer(this.timeOut)
                        //    .LogInformation(context => $"WORKFLOW {GetOrder(context).Id} TIMEOUT--")
                        //    .Then(context => StoreStatus(context, DemoHttpWorkflowStatus.Rejected))
                        //    .Then(StoreApproveTimeoutComment)
                        //    .SetVariable("RejectedBy", "Timer")
                        //    .Then("Join");
                    })
                .Add<Join>(x => x.WithMode(Join.JoinMode.WaitAny)).WithName("Join")

                //.Then<Fork>(
                //    fork => fork.WithBranches("Approve", "Reject", "Timer"),
                //    fork =>
                //    {
                //        fork
                //            .When("Approve").SetName("Approve02")
                //            .SignalReceived("approve")
                //            //.Then(ThrowErrorWhenStatusNotNew)
                //            .LogInformation(context => $"WORKFLOW {GetOrder(context).Id} APPROVED01++ (UserId={context.GetVariable<string>("UserId")})")
                //            .Then(context => StoreStatus(context, DemoHttpWorkflowStatus.Approved))
                //            .Then(StoreComment)
                //            //.Then(ThrowErrorIfFinished)
                //            .LogInformation(context => $"APPROVED: {GetOrder(context).Id}")
                //            .Then("Join02");

                //        fork
                //            .When("Reject").SetName("Reject03")
                //            .SignalReceived("reject")
                //            //.Then(ThrowErrorWhenStatusNotNew)
                //            .LogInformation(context => $"WORKFLOW {GetOrder(context).Id} REJECTED02-- (UserId={context.GetVariable<string>("UserId")})")
                //            .Then(context => StoreStatus(context, DemoHttpWorkflowStatus.Rejected))
                //            .Then(StoreComment)
                //            //.Then(ThrowErrorIfFinished)
                //            .LogInformation(context => $"REJECTED: {GetOrder(context).Id}")
                //            .Then("Join02");

                //        //fork
                //        //    .When("Timer")
                //        //    .Timer(this.timeOut)
                //        //    .LogInformation(context => $"WORKFLOW {GetOrder(context).Id} TIMEOUT--")
                //        //    .Then(context => StoreStatus(context, DemoHttpWorkflowStatus.Rejected))
                //        //    .Then(StoreApproveTimeoutComment)
                //        //    .SetVariable("RejectedBy", "Timer")
                //        //    .Then("Join02");
                //    })
                //.Add<Join>(x => x.WithMode(Join.JoinMode.WaitAny)).WithName("Join02")

                .LogInformation(context => $"Finished order for {GetOrder(context).Name} ({GetOrder(context).Email}, Status={GetState(context).Status})")
                .Then(context => StoreStatus(context, DemoHttpWorkflowStatus.Done))
                .Finish()
                .PersistWorkflow();
        }

        private string GetCorrelationId(ActivityExecutionContext context) => context.WorkflowExecutionContext.CorrelationId;

        private static WorkflowState GetState(ActivityExecutionContext context) => context.GetWorkflowContext<WorkflowState>();

        private static Order GetOrder(ActivityExecutionContext context) => GetState(context).Order;

        private static ICollection<Comment> GetComments(ActivityExecutionContext context) => GetState(context).Comments;

        private static void StoreStatus(ActivityExecutionContext context, DemoHttpWorkflowStatus status)
        {
            var state = (WorkflowState)context.WorkflowExecutionContext.WorkflowContext!;
            state.Status = status;
            context.SetVariable("Status", status);
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

        private static void ThrowErrorWhenStatusNotNew(ActivityExecutionContext context)
        {
            var canApprove = GetState(context).Status == DemoHttpWorkflowStatus.New;
            if (!canApprove)
            {
                throw new Exception($"Cannot approve/reject with order status {GetState(context).Status}");
            }
        }
    }
}