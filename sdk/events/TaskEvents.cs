using System;
using SDEIntegration.sdk.dto;

namespace SDEIntegration.sdk.events
{
    /// <summary>
    /// This class is to be used as a component to provide
    /// issue events for internal use. If this component
    /// is used it will allow an easy way for an integration
    /// writer to trigger and register to task events
    /// </summary>
    public class TaskEvents
    {
        public delegate void OnTaskCreateHook(SDETask task);
        public event OnTaskCreateHook onTaskCreateHook;
        
        public delegate void OnTaskUpdateHook(SDETask task);
        public event OnTaskUpdateHook onTaskUpdateHook;

        public delegate void OnTaskNoteCreateHook(SDETaskNote task);
        public event OnTaskNoteCreateHook onTaskNoteCreateHook;

        public void TriggerTaskCreated(SDETask task) {
            onTaskCreateHook(task);
        }

        public void TriggerTaskUpdate(SDETask task) {
            onTaskUpdateHook(task);
        }

        public void TriggerTaskNoteCreate(SDETaskNote note) {
            onTaskNoteCreateHook(note);
        }

        // Threadsafe Singleton IssueEvents (component)
        private static Lazy<TaskEvents> instance = new Lazy<TaskEvents>(() => new TaskEvents());
        public static TaskEvents Instance => instance.Value;
    }
}