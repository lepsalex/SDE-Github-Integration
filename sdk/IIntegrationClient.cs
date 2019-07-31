namespace SDEIntegration {
  public interface IIntegrationClient<T> where T : class
  {
      T CreateIssue(sdk.proto.Task task);
      T UpdateIssue(sdk.proto.Task task);
  }
}