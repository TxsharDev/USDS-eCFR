namespace EcfrAnalyzer.Models;

public class Agency
{
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string CfrTitlesJson { get; set; } = "[]";
    public int WordCount { get; set; }
    public string Checksum { get; set; } = string.Empty;
    public double RbiScore { get; set; }
    public double Complexity { get; set; }
    public DateTime? LastUpdated { get; set; }
}
