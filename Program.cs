using Serilog;
using dotenv.net;
using SDEIntegration.sdk;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;

namespace GithubIntegration
{
    class Program
    {
        static void Main(string[] args)
        {
            DotEnv.Config();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            Log.Information("Program Start!");

            var githubClient = new Github();
            var sdeClient = new SDEClient(githubClient);

            var tasks = new List<Task>();
            tasks.Add(RunSDEClient(sdeClient));
            tasks.Add(RunWebHost(args));

            Task.WaitAll(tasks.ToArray());

            Log.CloseAndFlush();
        }

        static Task RunSDEClient(SDEClient sdeClient)
        {
            return Task.Factory.StartNew(() =>
            {
                sdeClient.Run();
            });
        }

        static Task RunWebHost(string[] args)
        {
            return Task.Factory.StartNew(() =>
            {
                CreateWebHostBuilder(args).Build().Run();
            });
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
