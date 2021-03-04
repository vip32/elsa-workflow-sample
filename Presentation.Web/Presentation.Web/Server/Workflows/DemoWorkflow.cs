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
    public class DemoWorkflow : IWorkflow
    {
        private readonly IClock _clock;
        private readonly Duration _timeOut;

        public DemoWorkflow(IClock clock)
        {
            this._clock = clock;
            this._timeOut = Duration.FromSeconds(10);
        }

        public void Build(IWorkflowBuilder builder)
        {

            builder
                .WriteLine(context =>
                    $"The demo completes in {this._timeOut.ToString()} ({this._clock.GetCurrentInstant().Plus(this._timeOut)}). Can't wait that long? Send me the secret \"hurry\" signal! (http://localhost:7304/signal/hurry/trigger?correlationId={this.GetCorrelationId(context)})")
                .Then<Fork>(
                    fork => fork.WithBranches("Timer", "Signal"),
                    fork =>
                    {
                        fork
                            .When("Timer")
                            .Timer(this._timeOut)
                            .SetVariable("CompletedVia", "Timer")
                            .Then("Join");

                        fork
                            .When("Signal")
                            .SignalReceived("hurry")
                            .SetVariable("CompletedVia", "Signal")
                            .Then("Join");
                    })
                .Add<Join>(x => x.WithMode(Join.JoinMode.WaitAny)).WithName("Join")
                .WriteLine(context => $"Demo {this.GetCorrelationId(context)} completed successfully via {context.GetVariable<string>("CompletedVia")}!");
        }

        private string GetCorrelationId(ActivityExecutionContext context) => context.WorkflowExecutionContext.CorrelationId;
    }
}