using System;
using Octokit;
using SDEIntegration.sdk.dto;
using SDEIntegration.sdk.events;
using Serilog;

namespace GithubIntegration
{
    public class Hooks
    {
        TaskEvents sdeClientEvents;

        public Hooks()
        {
            sdeClientEvents = TaskEvents.Instance;   
        }

        public async void Process(IssueEventPayload payload)
        {

            // verify that sender is a github user and not our own events
            Log.Information("Sender: ", payload.Sender);
            
            // process the hooks based in action type
            Log.Information("Action: ", payload.Action);

            // extract what we need from the issue/repository
            // in order to trigger our events
            Log.Information("Issue: ", payload.Issue);
            Log.Information("Repository: ", payload.Repository);

            // trigger events
            // sdeClientEvents.TriggerTaskCreated();
            // sdeClientEvents.TriggerTaskUpdate();
            // sdeClientEvents.TriggerTaskNoteCreate();
        }
    }
}
