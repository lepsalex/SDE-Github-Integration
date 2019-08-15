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
  }
}