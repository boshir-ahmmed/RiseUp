namespace RiseUp.DataAccess.Services;

public sealed class AiOptions
{
    /// <summary>"Sandbox" uses the deterministic demo models below. A real Gemini adapter is plugged in here.</summary>
    public string Provider { get; set; } = "Sandbox";
    public string? GeminiApiKey { get; set; }
    /// <summary>Simulate outages in Development to exercise fail-closed and fallback paths.</summary>
    public bool SimulateModel1Outage { get; set; }
    public bool SimulateModel2Outage { get; set; }
    public int TimeoutSeconds { get; set; } = 8;
}
