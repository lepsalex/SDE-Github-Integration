using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Octokit;

namespace SDEGithubIntegration
{
  public class Github : IIntegrationClient<Task<Issue>>
  {
    GitHubClient githubClient;
    string githubUser;
    const string DEFAULT_REPO = "SDE-Github-Integration";

    private readonly Dictionary<string, string> projectToRepoMapping = new Dictionary<string, string>() {
      {"awesome", DEFAULT_REPO},
      {"possum", DEFAULT_REPO},
      {"murder", DEFAULT_REPO},
      {"9001", DEFAULT_REPO}
    };

    public Github()
    {
      githubClient = new GitHubClient(new ProductHeaderValue("github-integration"));
      githubUser = Environment.GetEnvironmentVariable("githubUser");

      if (githubUser == null)
      {
        throw new NullReferenceException("No githubUser found in env!");
      }

      var token = Environment.GetEnvironmentVariable("githubToken");

      if (token == null)
      {
        throw new NullReferenceException("No githubToken found in env!");
      }

      var tokenAuth = new Credentials(token); // use get env
      githubClient.Credentials = tokenAuth;
    }

    public async Task<Issue> CreateIssue(SDETask task)
    {
      var createIssue = new NewIssue(title: task.title);
      createIssue.Body = task.description;
      createIssue.Labels.Add(task.project);

      var issue = await githubClient.Issue.Create(
        owner: githubUser,
        name: GetRepoName(task.project),
        newIssue: createIssue);

      return issue;
    }

    public async Task<Issue> UpdateIssue(SDETask task)
    {
      var issue = await FindIssueFromTask(task);

      // If the issue is found, update it
      if (issue != null)
      {
        var update = issue.ToUpdate();

        update.Body = task.description;

        if (task.status.Equals("Complete"))
        {
          update.State = ItemState.Closed;
        }
        else
        {
          update.State = ItemState.Open;
        }

        return await githubClient.Issue.Get(githubUser, GetRepoName(task.project), issue.Id);
      }

      // if no issue is found, create the issue
      return await CreateIssue(task);
    }

    private async Task<Issue> FindIssueFromTask(SDETask task)
    {
      var taskIssueRequest = new RepositoryIssueRequest
      {
        Filter = IssueFilter.All,
        State = ItemStateFilter.All,
      };

      // Filter only for our project issues
      taskIssueRequest.Labels.Add(task.project);

      IReadOnlyList<Issue> issuesReadOnly = await githubClient.Issue.GetAllForRepository(githubUser, GetRepoName(task.project), taskIssueRequest);
      List<Issue> issues = (List<Issue>)issuesReadOnly;

      return issues.Find(issue => issue.Title.Equals(task.title));
    }

    private string GetRepoName(string project)
    {
      string value;
      return projectToRepoMapping.TryGetValue(project, out value) ? value : DEFAULT_REPO;
    }
  }
}