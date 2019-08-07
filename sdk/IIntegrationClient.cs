namespace SDEIntegration {
  public interface IIntegrationClient<T, I>
  {
      T CreateIssue(sdk.proto.Task task);
      T UpdateIssue(sdk.proto.Task task);
      I CreateIssueNote(sdk.proto.TaskNote taskNote);
  }
}