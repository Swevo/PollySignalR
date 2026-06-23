namespace PollySignalR;

/// <summary>
/// Extension methods for <see cref="HubConnection"/> that wrap lifecycle methods
/// with a Polly v8 resilience pipeline.
/// </summary>
public static class HubConnectionExtensions
{
    /// <summary>
    /// Starts the <see cref="HubConnection"/> with the supplied Polly v8
    /// <see cref="ResiliencePipeline"/> handling transient failures on the initial connect.
    /// </summary>
    /// <param name="connection">The hub connection to start.</param>
    /// <param name="pipeline">
    /// The Polly resilience pipeline to use. Pass <see cref="ResiliencePipeline.Empty"/>
    /// for no resilience (equivalent to calling <c>StartAsync</c> directly).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <example>
    /// <code>
    /// var pipeline = new ResiliencePipelineBuilder()
    ///     .AddRetry(new RetryStrategyOptions
    ///     {
    ///         MaxRetryAttempts = 5,
    ///         BackoffType = DelayBackoffType.Exponential,
    ///         Delay = TimeSpan.FromSeconds(1),
    ///         ShouldHandle = new PredicateBuilder().Handle&lt;Exception&gt;(),
    ///     })
    ///     .Build();
    ///
    /// await connection.StartWithResilienceAsync(pipeline);
    /// </code>
    /// </example>
    public static Task StartWithResilienceAsync(
        this HubConnection connection,
        ResiliencePipeline pipeline,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(pipeline);

        return pipeline
            .ExecuteAsync(ct => new ValueTask(connection.StartAsync(ct)), cancellationToken)
            .AsTask();
    }
}
