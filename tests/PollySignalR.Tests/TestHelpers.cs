namespace PollySignalR.Tests;

/// <summary>
/// Creates <see cref="RetryContext"/> values for testing.
/// </summary>
internal static class RetryContextFactory
{
    public static RetryContext Create(long previousRetryCount, Exception? reason = null)
        => new RetryContext
        {
            PreviousRetryCount = previousRetryCount,
            ElapsedTime = TimeSpan.Zero,
            RetryReason = reason ?? new Exception("test"),
        };
}
