using TaskNoteProto = SDEIntegration.sdk.proto.TaskNote;

namespace SDEIntegration.sdk.dto
{
    public class SDETaskNote
    {
        public string Project { get; set; }
        public string Title { get; set; }
        public string Note { get; set; }

        public SDETaskNote(string project, string title, string note)
        {
            this.Project = project;
            this.Title = title;
            this.Note = note;
        }

        // Auto-convert task note objects to proto implicitly
        public static implicit operator TaskNoteProto(SDETaskNote task)
        {
            return new TaskNoteProto()
            {
              TaskProject = task.Project,
              TaskTitle = task.Title,
              NewNote = task.Note
            };
        }
    }
}
