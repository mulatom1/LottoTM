namespace LottoTM.Server.Api.Options;

public class LottoWorkerOptions
{
    public bool Enable { get; set; } = false;
    public TimeSpan StartTime { get; set; } = new TimeSpan(22, 0, 0);
    public TimeSpan EndTime { get; set; } = new TimeSpan(23, 0, 0);
    public int IntervalMinutes { get; set; } = 5;
    public List<string> InWeek { get; set; } = new List<string> { "WT", "SR", "SO" };
}
