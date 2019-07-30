using Serilog;
using dotenv.net;

namespace SDEGithubIntegration
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

            sdeClient.run();

            Log.CloseAndFlush();
        }
    }
}
