using System;
using SDEIntegration.sdk.dto;
using SDEIntegration.sdk.events;

namespace GithubIntegration
{
    public class HookListener
    {
        TaskEvents sdeClientEvents;

        public HookListener()
        {
            sdeClientEvents = TaskEvents.Instance;   
        }

        // Placholder endpoint
        void HookEndpoint()
        {
            // Get data

            // trigger events
            // sdeClientEvents.TriggerTaskCreated();
            // sdeClientEvents.TriggerTaskUpdate();
            // sdeClientEvents.TriggerTaskNoteCreate();
        }
    }
}
