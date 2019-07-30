using System.Threading.Tasks;
using Serilog;

namespace SDEGithubIntegration
{
  public class SDEClient
  {
    IIntegrationClient<Task<SDEIssue>> integrationClient;

    public SDEClient(IIntegrationClient<Task<SDEIssue>> integrationClient)
    {
      this.integrationClient = integrationClient;
    }

    public void run() {
      Log.Information("SDE Client Start ...");
    }
  }
}