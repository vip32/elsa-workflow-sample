using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Presentation.Web.Server
{
    /// <summary>
    /// A simple worker that starts a workflow
    /// </summary>
    public class WorkflowStarter<T> : IHostedService where T : IWorkflow
    {
        private readonly IServiceProvider serviceProvider;

        public WorkflowStarter(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = this.serviceProvider.CreateScope();
            var workflowRunner = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();
            await workflowRunner.RunWorkflowAsync<T>(correlationId: Guid.NewGuid().ToString("N"), cancellationToken: cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}