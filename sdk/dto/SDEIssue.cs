namespace SDEGithubIntegration
{
  public class SDEIssue
  {
    public long Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Status { get; set; }

    public SDEIssue(long id, string title, string description, string status)
    {
      this.Id = id;
      this.Title = title;
      this.Description = description;
      this.Status = status;
    }
  }
}