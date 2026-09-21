using System.Diagnostics;

namespace Drive.Application.Common.Telemetry;

/// <summary>
/// Tập trung khai báo tất cả ActivitySource dùng trong Application layer.
/// </summary>
public static class ActivitySources
{
    /// <summary>
    /// ActivitySource cho Application layer (MediatR handlers).
    /// Được đăng ký vào OpenTelemetry tại Program.cs qua .AddSource("Drive.Application").
    /// </summary>
    public static readonly ActivitySource Application = new("Drive.Application");

    /// <summary>
    /// ActivitySource cho Infrastructure layer (Repository calls).
    /// Được đăng ký vào OpenTelemetry tại Program.cs qua .AddSource("Drive.Infrastructure").
    /// </summary>
    public static readonly ActivitySource Infrastructure = new("Drive.Infrastructure");
}
