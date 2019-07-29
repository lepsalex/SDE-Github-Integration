namespace SDEGithubIntegration {
  public interface IIntegrationClient<T>
  {
      T CreateIssue(SDETask task);
      T UpdateIssue(SDETask task);
  }
}