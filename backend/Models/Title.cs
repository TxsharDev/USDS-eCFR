namespace EcfrAnalyzer.Models;

public class Title
{
    public int Number { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LatestAmended { get; set; }
    public string? UpToDateAsOf { get; set; }
    public bool Reserved { get; set; }
    public DateTime? LastFetched { get; set; }
}
