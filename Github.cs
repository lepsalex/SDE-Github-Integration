using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Octokit;
using SDEIntegration.sdk.dto;
using Serilog;

namespace SDEIntegration
{
  public class Github : IIntegrationClient<Task<SDEIssue>>
  {
    GitHubClient githubClient;
    string githubUser;
    const string DEFAULT_REPO = "lepsalex/SDE-Github-Integration";

    private readonly Dictionary<string, string> ProjectToRepoMapping = new Dictionary<string, string>() {
      {"awesome", DEFAULT_REPO},
      {"possum", DEFAULT_REPO},
      {"murder", DEFAULT_REPO},
      {"9001", DEFAULT_REPO}
    };

    public Github()
    {
      Log.Information("Github Client Start ...");

      githubClient = new GitHubClient(new ProductHeaderValue("github-integration"));
      githubUser = Environment.GetEnvironmentVariable("GITHUB_USER");

      if (githubUser == null)
      {
        throw new NullReferenceException("No githubUser found in env!");
      }

      var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");

      if (token == null)
      {
        throw new NullReferenceException("No githubToken found in env!");
      }

      var tokenAuth = new Credentials(token); // use get env
      githubClient.Credentials = tokenAuth;

      Log.Information("Github client connected!");
    }

    public async Task<SDEIssue> CreateIssue(sdk.proto.Task task)
    {
      var createIssue = new NewIssue(title: task.Title);
      createIssue.Body = task.Description;
      createIssue.Labels.Add(task.Project);

      var issue = await githubClient.Issue.Create(
        owner: githubUser,
        name: GetRepoName(task.Project),
        newIssue: createIssue);

      return CreateSDEIssue(issue);
    }

    public async Task<SDEIssue> UpdateIssue(sdk.proto.Task task)
    {
      var issue = await FindIssueFromTask(task);

      // If the issue is found, update it
      if (issue != null)
      {
        var update = issue.ToUpdate();

        update.Body = task.Description;

        if (task.Status.Equals("Complete"))
        {
          update.State = ItemState.Closed;
        }
        else
        {
          update.State = ItemState.Open;
        }

        var updatedIssue = await githubClient.Issue.Get(githubUser, GetRepoName(task.Project), issue.Id);

        return CreateSDEIssue(updatedIssue);
      }

      // if no issue is found, create the issue
      return await CreateIssue(task);
    }

    private async Task<Issue> FindIssueFromTask(sdk.proto.Task task)
    {
      var taskIssueRequest = new RepositoryIssueRequest
      {
        Filter = IssueFilter.All,
        State = ItemStateFilter.All,
      };

      // Filter only for our Project issues
      taskIssueRequest.Labels.Add(task.Project);

      IReadOnlyList<Issue> issuesReadOnly = await githubClient.Issue.GetAllForRepository(githubUser, GetRepoName(task.Project), taskIssueRequest);
      List<Issue> issues = (List<Issue>)issuesReadOnly;

      return issues.Find(issue => issue.Title.Equals(task.Title));
    }

    private string GetRepoName(string Project)
    {
      string value;
      return ProjectToRepoMapping.TryGetValue(Project, out value) ? value : DEFAULT_REPO;
    }

    private SDEIssue CreateSDEIssue(Issue issue) {
      return new SDEIssue(issue.Id, issue.Title, issue.Body, issue.State.ToString());
    }
  }
}