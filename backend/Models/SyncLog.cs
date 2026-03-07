namespace EcfrAnalyzer.Models;

public class SyncLog
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = "running";
    public int AgenciesSynced { get; set; }
    public string? ErrorMessage { get; set; }
}
