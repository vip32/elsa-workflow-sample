using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Elsa;
using Elsa.Persistence.YesSql;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.Sqlite;
using Microsoft.EntityFrameworkCore;
using YesSql.Provider.Sqlite;

namespace Presentation.Web.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddElsa(options => options
                    .UseYesSqlPersistence()
                    //.UseYesSqlPersistence(c =>
                    //    c.UseSqLite("Data Source=elsa_ys.db;Cache=Shared"))
                    //.UseEntityFrameworkPersistence(c =>
                    //{
                    //    c.UseSqlite(
                    //        "Data Source=elsa_ef.db;Cache=Shared",
                    //        db => db.MigrationsAssembly(typeof(SqliteElsaContextFactory).Assembly.GetName().Name));
                    //}, true)
                    .AddConsoleActivities()
                    .AddHttpActivities()
                    .AddQuartzTemporalActivities()
                    .AddWorkflow<HelloHttpWorkflow>()
                    .AddWorkflow<DemoHttpWorkflow>()
                    .AddWorkflow<DemoWorkflow>()                )
                .AddDataMigration<Migrations>()
                .AddIndexProvider<WorkflowStateIndexProvider>()
                .AddWorkflowContextProvider<WorkflowStateProvider>(); ;
                //.AddHostedService<WorkflowStarter<DemoWorkflow>>();

            services.AddControllersWithViews();
            services.AddRazorPages();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpActivities(); // elsa
            app.UseHttpsRedirection();
            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}
