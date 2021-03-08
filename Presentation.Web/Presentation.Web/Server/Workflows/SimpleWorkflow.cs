using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Primitives;
using Elsa.Activities.Temporal;
using Elsa.Builders;
using Elsa.Services.Models;
using NodaTime;

namespace Presentation.Web.Server
{
    /// <summary>
    /// Demonstrates the Fork, Join, Timer and Signal activities working together to model a long-running process where either the timer causes the workflow to resume, or a signal.
    /// </summary>
    public class SimpleWorkflow : IWorkflow
    {
        private readonly IClock clock;
        private readonly Duration timeOut;

        public SimpleWorkflow(IClock clock)
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

                .WriteLine(context =>
                    $"The simple demo completes in {this.timeOut.ToString()} ({this.clock.GetCurrentInstant().Plus(this.timeOut)}). Can't wait that long? Send me the secret \"hurry\" signal! (http://localhost:7304/signal/hurry/trigger?correlationId={this.GetCorrelationId(context)})")
                .Then<Fork>(
                    fork => fork.WithBranches("Timer", "Signal"),
                    fork =>
                    {
                        fork
                            .When("Timer")
                            .Timer(this.timeOut)
                            .SetVariable("CompletedVia", "Timer")
                            .Then("Join");

                        fork
                            .When("Signal")
                            .SignalReceived("hurry")
                            .SetVariable("CompletedVia", "Signal")
                            .Then("Join");
                    })
                .Add<Join>(x => x.WithMode(Join.JoinMode.WaitAny)).WithName("Join")
                .WriteLine(context => $"Simple {this.GetCorrelationId(context)} completed successfully via {context.GetVariable<string>("CompletedVia")}!")
                .Finish()
                .PersistWorkflow();
        }

        private string GetCorrelationId(ActivityExecutionContext context) => context.WorkflowExecutionContext.CorrelationId;
    }
}