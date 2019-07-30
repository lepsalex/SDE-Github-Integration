namespace SDEGithubIntegration {
  public interface IIntegrationClient<T> where T : class
  {
      T CreateIssue(SDETask task);
      T UpdateIssue(SDETask task);
  }
}