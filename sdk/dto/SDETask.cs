using TaskProto = SDEIntegration.sdk.proto.Task;

namespace SDEIntegration.sdk.dto
{
    public class SDETask
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }

        public SDETask(long id, string title, string description, string status)
        {
            this.Id = id;
            this.Title = title;
            this.Description = description;
            this.Status = status;
        }

        // Auto-convert task objects to proto implicitly
        public static implicit operator TaskProto(SDETask task)
        {
            return new TaskProto(){
              Id = (ulong) task.Id,
              Title = task.Title,
              Description = task.Description,
              Status = task.Status
            };
        }
    }
}
