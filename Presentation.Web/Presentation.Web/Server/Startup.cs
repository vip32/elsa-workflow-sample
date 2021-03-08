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
using ElsaDashboard.Backend.Extensions;
using System;
using Elsa.Persistence.EntityFramework.SqlServer;
using Microsoft.Extensions.Logging;

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
                    .UseYesSqlPersistence(c =>
                        c.UseSqLite("Data Source=elsa_ys.db;Cache=Shared"))
                    //.UseEntityFrameworkPersistence(c =>
                    //{
                    //    c.UseSqlite(
                    //        "Data Source=elsa_ef.db;Cache=Shared",
                    //        db => db.MigrationsAssembly(typeof(SqliteElsaContextFactory).Assembly.GetName().Name));
                    //}, true)
                    //.UseEntityFrameworkPersistence(c =>
                    //{
                    //    c.UseSqlServer(
                    //        "Server=127.0.0.1,14338;Database=elsa;User=sa;Password=Abcd1234!;Trusted_Connection=false;",
                    //        db => db
                    //            .MigrationsAssembly(typeof(SqlServerElsaContextFactory).Assembly.GetName().Name)
                    //            .MigrationsHistoryTable("__MigrationsHistory", schema: "Elsa")
                    //            .EnableRetryOnFailure())
                    //      .UseLoggerFactory(services.BuildServiceProvider().GetRequiredService<ILoggerFactory>())
                    //      .EnableSensitiveDataLogging()
                    //     .EnableDetailedErrors();
                    //}, true)
                    .AddConsoleActivities()
                    .AddHttpActivities(this.Configuration.GetSection("Elsa").GetSection("Http").Bind)
                    .AddEmailActivities(this.Configuration.GetSection("Elsa").GetSection("Smtp").Bind)
                    .AddQuartzTemporalActivities()
                    .AddActivitiesFrom<Program>()
                    .AddWorkflowsFrom<Program>())
                .AddDataMigration<Migrations>()
                .AddIndexProvider<WorkflowStateIndexProvider>()
                .AddWorkflowContextProvider<WorkflowStateProvider>(); ;
            //.AddHostedService<WorkflowStarter<DemoWorkflow>>();

            services
                .AddElsaApiEndpoints()
                .AddElsaSwagger();

            services.AddControllersWithViews();
            services.AddRazorPages();

            services.AddElsaDashboardUI();
            services.AddElsaDashboardBackend(options => options.ServerUrl = this.Configuration.GetValue<Uri>("Elsa:Http:BaseUrl"));
            services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));
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

            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Elsa"));

            app.UseHttpActivities(); // elsa
            app.UseCors();
            app.UseHttpsRedirection();
            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseElsaGrpcServices(); // elsa

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}
