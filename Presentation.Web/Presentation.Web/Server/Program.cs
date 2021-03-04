namespace Presentation.Web.Server
{
    using System.IO;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Serilog;

    public static class Program
    {
        private static readonly string AppName = typeof(Program).Namespace;

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
           Host.CreateDefaultBuilder(args)
               .ConfigureAppConfiguration((context, builder) =>
               {
                   builder
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{context?.HostingEnvironment?.EnvironmentName}.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables();
               })
               .UseSerilog((context, builder) =>
               {
                   builder.ReadFrom.Configuration(context.Configuration)
                     .MinimumLevel.Debug()
                     .Enrich.WithProperty("ServiceName", AppName)
                     .Enrich.FromLogContext()
                     // TODO: applicationinsights, not available on linux app service plan
                     .WriteTo.Trace(Serilog.Events.LogEventLevel.Information)
#if DEBUG
                     .WriteTo.Seq(string.IsNullOrWhiteSpace(context.Configuration["Serilog:SeqServerUrl"]) ? "http://localhost:5341" /*"http://seq"*/ : context.Configuration["Serilog:SeqServerUrl"])
#endif
                     .WriteTo.Console(Serilog.Events.LogEventLevel.Information);
               })
               .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
    }
}