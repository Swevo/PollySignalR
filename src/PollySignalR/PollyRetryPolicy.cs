namespace PollySignalR;

/// <summary>
/// A SignalR <see cref="IRetryPolicy"/> backed by Polly v8 exponential back-off with jitter.
/// Drop-in replacement for <c>WithAutomaticReconnect(TimeSpan[])</c>.
/// </summary>
public sealed class PollyRetryPolicy : IRetryPolicy
{
    private readonly PollySignalROptions _options;
    private readonly Random _rng;

    /// <summary>
    /// Initialises the policy with the given options.
    /// </summary>
    public PollyRetryPolicy(PollySignalROptions options) : this(options, Random.Shared) { }

    internal PollyRetryPolicy(PollySignalROptions options, Random rng)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.BaseDelay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options), "BaseDelay must be non-negative.");
        if (options.MaxDelay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options), "MaxDelay must be non-negative.");
        if (options.JitterFactor < 0 || options.JitterFactor > 1)
            throw new ArgumentOutOfRangeException(nameof(options), "JitterFactor must be between 0 and 1.");

        _options = options;
        _rng = rng;
    }

    /// <inheritdoc />
    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        if (_options.MaxRetries.HasValue && retryContext.PreviousRetryCount >= _options.MaxRetries.Value)
            return null;

        var baseMs = _options.BaseDelay.TotalMilliseconds;
        var maxMs = _options.MaxDelay.TotalMilliseconds;

        double computed = _options.BackoffType switch
        {
            DelayBackoffType.Linear => baseMs * (retryContext.PreviousRetryCount + 1),
            _ => baseMs * Math.Pow(2.0, retryContext.PreviousRetryCount),
        };

        var capped = Math.Min(computed, maxMs);

        // Add ±JitterFactor of the current base delay as noise.
        var jitterRange = baseMs * _options.JitterFactor;
        var jitter = (_rng.NextDouble() * 2.0 - 1.0) * jitterRange;

        var finalMs = Math.Max(0.0, capped + jitter);
        return TimeSpan.FromMilliseconds(finalMs);
    }
}
