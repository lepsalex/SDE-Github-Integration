using System;
using System.Threading.Tasks;

namespace SDEGithubIntegration
{
  public class SDEClient
  {
    IIntegrationClient<Task<SDEIssue>> integrationClient;

    public SDEClient(IIntegrationClient<Task<SDEIssue>> integrationClient)
    {
      this.integrationClient = integrationClient;
    }
  }
}