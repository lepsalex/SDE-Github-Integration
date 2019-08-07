namespace SDEIntegration.sdk.dto
{
  public class SDENote
  {
    public string Project { get; set; }
    public string Title { get; set; }
    public string Note { get; set; }

    public SDENote(string project, string title, string note)
    {
      this.Project = project;
      this.Title = title;
      this.Note = note;
    }
  }
}