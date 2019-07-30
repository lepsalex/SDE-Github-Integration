namespace SDEGithubIntegration
{
  public class SDETask
  {
    public long id { get; set; }
    public string project { get; set; }
    public string title { get; set; }
    public string description { get; set; }
    public string status { get; set; }

    public SDETask(long id, string project, string title, string description, string status)
    {
      this.id = id;
      this.project = project;
      this.title = title;
      this.description = description;
      this.status = status;
    }
  }
}