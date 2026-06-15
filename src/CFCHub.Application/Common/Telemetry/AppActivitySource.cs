using System.Diagnostics;

namespace CFCHub.Application.Common.Telemetry;

public static class AppActivitySource
{
    public static readonly string Name = "CFCHub.Application";
    public static readonly ActivitySource Instance = new(Name);
}
