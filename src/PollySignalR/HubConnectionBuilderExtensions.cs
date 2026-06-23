namespace PollySignalR;

/// <summary>
/// Extension methods for <see cref="IHubConnectionBuilder"/> to add Polly v8 reconnect behaviour.
/// </summary>
public static class HubConnectionBuilderExtensions
{
    /// <summary>
    /// Configures the <see cref="HubConnection"/> to automatically reconnect using a
    /// Polly v8 exponential back-off policy with jitter.
    /// </summary>
    /// <param name="builder">The hub connection builder.</param>
    /// <param name="configure">Optional delegate to customise reconnect options.</param>
    /// <returns>The original builder to allow chaining.</returns>
    /// <example>
    /// <code>
    /// var connection = new HubConnectionBuilder()
    ///     .WithUrl("https://example.com/hub")
    ///     .WithPollyReconnect(options =>
    ///     {
    ///         options.MaxRetries = 10;
    ///         options.BaseDelay = TimeSpan.FromSeconds(2);
    ///         options.MaxDelay = TimeSpan.FromSeconds(120);
    ///     })
    ///     .Build();
    /// </code>
    /// </example>
    public static IHubConnectionBuilder WithPollyReconnect(
        this IHubConnectionBuilder builder,
        Action<PollySignalROptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new PollySignalROptions();
        configure?.Invoke(options);

        return builder.WithAutomaticReconnect(new PollyRetryPolicy(options));
    }
}
