using System.Threading.Tasks;

namespace SDEIntegration {
  public interface IIntegrationClient<T, I>
  {
      Task<T> CreateIssue(sdk.proto.Task task);
      Task<T> UpdateIssue(sdk.proto.Task task);
      Task<I> CreateIssueNote(sdk.proto.TaskNote taskNote);

      void OnIssueCreateHook(T issue);
      void OnIssueUpdateHook(T issue);
      void OnIssueNoteCreateHook(I issue);
  }
}