using System.Threading.Tasks;

using SDETaskProto = SDEIntegration.sdk.proto.Task;
using SDETaskNoteProto = SDEIntegration.sdk.proto.TaskNote;

namespace SDEIntegration
{
    public interface IIntegrationClient<T, I>
    {
        string GroupId { get; }
        bool HooksEnabled { get; }

        Task<T> CreateTask(SDETaskProto task);
        Task<T> UpdateTask(SDETaskProto task);
        Task<T> RemoveTask(SDETaskProto task);
        Task<I> CreateTaskNote(SDETaskNoteProto taskNote);
    }
}