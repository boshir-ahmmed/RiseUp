namespace RiseUp.AI;

public sealed class AiOptions
{
    /// <summary>Simulate outages in Development to exercise fail-closed and fallback paths.</summary>
    public bool SimulateModel1Outage { get; set; }
    public bool SimulateModel2Outage { get; set; }
}
