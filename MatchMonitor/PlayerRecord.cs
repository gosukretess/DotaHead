using OpenDotaApi.Api.Matches.Model;

namespace DotaHead.MatchMonitor;

public class PlayerRecord
{
    public MatchPlayer Player { get; set; }
    public string Name { get; set; } = "Unknown";
    public Lane Lane { get; set; }
    public Team Team { get; set; }
    public Role Role { get; set; }
}