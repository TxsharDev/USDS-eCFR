namespace EcfrAnalyzer.Models;

public class Change
{
    public int Id { get; set; }
    public string AgencySlug { get; set; } = string.Empty;
    public int TitleNumber { get; set; }
    public string? Part { get; set; }
    public string? SectionName { get; set; }
    public string ChangeDate { get; set; } = string.Empty;
    public bool Substantive { get; set; } = true;
    public bool Removed { get; set; }
}
