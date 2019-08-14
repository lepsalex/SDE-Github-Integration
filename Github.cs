using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Octokit;
using SDEIntegration.sdk.dto;
using Serilog;

namespace SDEIntegration
{
    public class Github : IIntegrationClient<SDEIssue, SDENote>
    {
        GitHubClient githubClient;
        string githubUser;
        string defaultRepo;
        Dictionary<string, string> projectToRepoMapping;

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

            // Set default repo
            defaultRepo = Environment.GetEnvironmentVariable("DEFAULT_REPO");

            projectToRepoMapping = new Dictionary<string, string>() {
              {"awesome", defaultRepo},
              {"possum", defaultRepo},
              {"murder", defaultRepo},
              {"9001", defaultRepo}
            };

            // think about what happens when webhooks are not possible, how do we simulate this
            // registerHooks();

            Log.Information("Github client connected!");
        }

        public void registerHooks()
        {
            var hookConfig = new NewRepositoryHook("web", new Dictionary<string, string>() {
              {"url", "https://example.com/webhook"}, // host this webhook on this app (tbd)
              {"content_type", "json"},
              {"insecure_ssl", "0"}
            });

            hookConfig.Events = new HashSet<string>() { "issues", "issue_comment" };

            foreach (var repo in projectToRepoMapping.Values)
            {
                using (var hook = githubClient.Repository.Hooks.Create(githubUser, repo, hookConfig))
                {
                    // register some sort of hook delete when application shutsdown from here
                    // githubClient.Repository.Hooks.Delete(githubUser, defaultRepo, hook.Id);

                    // NOTE: when handling the hook event we should see if we can identify events coming
                    // from this application as to ignore them.
                }
            }
        }

        public async Task<SDEIssue> CreateIssue(sdk.proto.Task task)
        {
            var existingIssue = await FindIssue(task);

            // If the issue is found, update it instead of creating
            if (existingIssue != null)
            {
                return await UpdateIssue(task);
            }
            else
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

        }

        public async Task<SDEIssue> UpdateIssue(sdk.proto.Task task)
        {
            var issue = await FindIssue(task);

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

                var updatedIssue = await githubClient.Issue.Update(githubUser, GetRepoName(task.Project), issue.Number, update);

                return CreateSDEIssue(updatedIssue);
            }

            // if no issue is found, create the issue
            return await CreateIssue(task);
        }

        public async Task<SDENote> CreateIssueNote(sdk.proto.TaskNote taskNote)
        {
            var issue = await FindIssue(taskNote.TaskProject, taskNote.TaskTitle);

            if (issue == null)
            {
                Log.Error($"No issue found for {taskNote.TaskProject} - {taskNote.TaskTitle}");
                return null;
            }

            var note = await githubClient.Issue.Comment.Create(githubUser, GetRepoName(taskNote.TaskProject), issue.Number, taskNote.NewNote);

            return CreateSDENote(taskNote.TaskProject, taskNote.TaskTitle, note);
        }

        public void OnIssueCreateHook(SDEIssue issue) {
            
        }

        public void OnIssueUpdateHook(SDEIssue issue) {
            
        }

        public void OnIssueNoteCreateHook(SDENote issue) {
            
        }

        private async Task<Issue> FindIssue(sdk.proto.Task task)
        {
            return await FindIssue(task.Project, task.Title);
        }

        private async Task<Issue> FindIssue(string project, string title)
        {
            var taskIssueRequest = new RepositoryIssueRequest
            {
                Filter = IssueFilter.All,
                State = ItemStateFilter.All,
            };

            // Filter only for our Project issues
            taskIssueRequest.Labels.Add(project);

            var issues = await githubClient.Issue.GetAllForRepository(githubUser, GetRepoName(project), taskIssueRequest);

            var newList = new List<Issue>(issues);

            return newList.Find(issue => issue.Title.Equals(title));
        }

        private string GetRepoName(string Project)
        {
            string value;
            return projectToRepoMapping.TryGetValue(Project, out value) ? value : defaultRepo;
        }

        private SDEIssue CreateSDEIssue(Issue issue)
        {
            return new SDEIssue(issue.Id, issue.Title, issue.Body, issue.State.ToString());
        }

        private SDENote CreateSDENote(string project, string title, IssueComment issueComment)
        {
            return new SDENote(project, title, issueComment.Body);
        }
    }
}