namespace SDEGithubIntegration
{
  public class SDEIssue
  {
    public long id { get; set; }
    public string title { get; set; }
    public string description { get; set; }
    public string status { get; set; }

    public SDEIssue(long id, string title, string description, string status)
    {
      this.id = id;
      this.title = title;
      this.description = description;
      this.status = status;
    }
  }
}