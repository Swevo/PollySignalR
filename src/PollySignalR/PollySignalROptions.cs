namespace PollySignalR;

/// <summary>
/// Configuration options for the Polly-backed SignalR reconnect policy.
/// </summary>
public sealed class PollySignalROptions
{
    /// <summary>
    /// Maximum number of reconnect attempts. <c>null</c> means retry indefinitely.
    /// Default: <c>null</c> (infinite).
    /// </summary>
    public int? MaxRetries { get; set; } = null;

    /// <summary>Base delay for the first retry (exponential back-off). Default: 1 second.</summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Maximum delay cap; no retry will wait longer than this. Default: 60 seconds.</summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Jitter factor applied to the computed delay to spread reconnect storms.
    /// A value of 0.5 adds ±50% of the base delay as random noise. Default: 0.5.
    /// </summary>
    public double JitterFactor { get; set; } = 0.5;

    /// <summary>
    /// Back-off type. <see cref="DelayBackoffType.Exponential"/> doubles the delay
    /// on each attempt; <see cref="DelayBackoffType.Linear"/> increases it linearly.
    /// Default: <see cref="DelayBackoffType.Exponential"/>.
    /// </summary>
    public DelayBackoffType BackoffType { get; set; } = DelayBackoffType.Exponential;
}
