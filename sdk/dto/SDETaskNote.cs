namespace SDEIntegration.sdk.dto
{
  public class SDETaskNote
  {
    public string Project { get; set; }
    public string Title { get; set; }
    public string Note { get; set; }

    public SDETaskNote(string project, string title, string note)
    {
      this.Project = project;
      this.Title = title;
      this.Note = note;
    }
  }
}