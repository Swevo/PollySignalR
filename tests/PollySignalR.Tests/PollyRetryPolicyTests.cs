namespace PollySignalR.Tests;

public class PollyRetryPolicyTests
{
    // Use a seeded Random so delay assertions are deterministic (jitter = 0 when seed = fixed)
    private static PollyRetryPolicy ZeroJitter(Action<PollySignalROptions>? configure = null)
    {
        var options = new PollySignalROptions { JitterFactor = 0 };
        configure?.Invoke(options);
        return new PollyRetryPolicy(options, new Random(42));
    }

    // ── Infinite retries (MaxRetries = null) ──────────────────────────────

    [Fact]
    public void MaxRetries_Null_NeverReturnsNull()
    {
        var policy = ZeroJitter(o => o.MaxRetries = null);

        for (long i = 0; i < 100; i++)
        {
            var delay = policy.NextRetryDelay(RetryContextFactory.Create(i));
            delay.Should().NotBeNull(because: $"attempt {i} should not stop");
        }
    }

    // ── MaxRetries cutoff ─────────────────────────────────────────────────

    [Fact]
    public void MaxRetries_ReturnsNullAtThreshold()
    {
        var policy = ZeroJitter(o => o.MaxRetries = 3);

        policy.NextRetryDelay(RetryContextFactory.Create(0)).Should().NotBeNull();
        policy.NextRetryDelay(RetryContextFactory.Create(1)).Should().NotBeNull();
        policy.NextRetryDelay(RetryContextFactory.Create(2)).Should().NotBeNull();
        policy.NextRetryDelay(RetryContextFactory.Create(3)).Should().BeNull("MaxRetries reached");
        policy.NextRetryDelay(RetryContextFactory.Create(4)).Should().BeNull("beyond MaxRetries");
    }

    [Fact]
    public void MaxRetries_Zero_AlwaysReturnsNull()
    {
        var policy = ZeroJitter(o => o.MaxRetries = 0);
        policy.NextRetryDelay(RetryContextFactory.Create(0)).Should().BeNull();
    }

    // ── Exponential back-off ──────────────────────────────────────────────

    [Fact]
    public void ExponentialBackoff_DoublesOnEachAttempt()
    {
        var policy = ZeroJitter(o =>
        {
            o.BaseDelay = TimeSpan.FromSeconds(1);
            o.MaxDelay = TimeSpan.FromMinutes(10);
            o.BackoffType = DelayBackoffType.Exponential;
        });

        var d0 = policy.NextRetryDelay(RetryContextFactory.Create(0))!.Value.TotalSeconds;
        var d1 = policy.NextRetryDelay(RetryContextFactory.Create(1))!.Value.TotalSeconds;
        var d2 = policy.NextRetryDelay(RetryContextFactory.Create(2))!.Value.TotalSeconds;

        d0.Should().BeApproximately(1.0, 0.01);   // 1s
        d1.Should().BeApproximately(2.0, 0.01);   // 2s
        d2.Should().BeApproximately(4.0, 0.01);   // 4s
    }

    // ── Linear back-off ───────────────────────────────────────────────────

    [Fact]
    public void LinearBackoff_IncreasesLinearly()
    {
        var policy = ZeroJitter(o =>
        {
            o.BaseDelay = TimeSpan.FromSeconds(5);
            o.MaxDelay = TimeSpan.FromMinutes(10);
            o.BackoffType = DelayBackoffType.Linear;
        });

        var d0 = policy.NextRetryDelay(RetryContextFactory.Create(0))!.Value.TotalSeconds;
        var d1 = policy.NextRetryDelay(RetryContextFactory.Create(1))!.Value.TotalSeconds;
        var d2 = policy.NextRetryDelay(RetryContextFactory.Create(2))!.Value.TotalSeconds;

        d0.Should().BeApproximately(5.0, 0.01);
        d1.Should().BeApproximately(10.0, 0.01);
        d2.Should().BeApproximately(15.0, 0.01);
    }

    // ── MaxDelay cap ──────────────────────────────────────────────────────

    [Fact]
    public void MaxDelay_CapsLargeDelays()
    {
        var policy = ZeroJitter(o =>
        {
            o.BaseDelay = TimeSpan.FromSeconds(1);
            o.MaxDelay = TimeSpan.FromSeconds(10);
            o.BackoffType = DelayBackoffType.Exponential;
        });

        // At attempt 10, exponential would be 1024s — must be capped at 10s
        var delay = policy.NextRetryDelay(RetryContextFactory.Create(10))!.Value;
        delay.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void MaxDelay_EqualToBaseDelay_AllDelaysAreCapped()
    {
        var policy = ZeroJitter(o =>
        {
            o.BaseDelay = TimeSpan.FromSeconds(5);
            o.MaxDelay = TimeSpan.FromSeconds(5);
        });

        for (long i = 0; i < 10; i++)
        {
            var delay = policy.NextRetryDelay(RetryContextFactory.Create(i))!.Value;
            delay.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(5));
        }
    }

    // ── Jitter ────────────────────────────────────────────────────────────

    [Fact]
    public void Jitter_ProducesVariedDelays()
    {
        var options = new PollySignalROptions
        {
            BaseDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromMinutes(10),
            JitterFactor = 0.5,
        };
        var policy = new PollyRetryPolicy(options, new Random()); // unseeded

        var delays = Enumerable.Range(0, 20)
            .Select(_ => policy.NextRetryDelay(RetryContextFactory.Create(0))!.Value.TotalMilliseconds)
            .ToList();

        delays.Distinct().Should().HaveCountGreaterThan(1, "jitter should produce varied delays");
    }

    [Fact]
    public void Jitter_NeverProducesNegativeDelay()
    {
        var options = new PollySignalROptions
        {
            BaseDelay = TimeSpan.FromMilliseconds(10),
            MaxDelay = TimeSpan.FromSeconds(10),
            JitterFactor = 1.0, // maximum jitter
        };
        var policy = new PollyRetryPolicy(options, new Random(1));

        for (long i = 0; i < 50; i++)
        {
            var delay = policy.NextRetryDelay(RetryContextFactory.Create(i))!.Value;
            delay.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
        }
    }

    // ── Defaults ──────────────────────────────────────────────────────────

    [Fact]
    public void DefaultOptions_InfiniteExponentialBackoff()
    {
        var policy = new PollyRetryPolicy(new PollySignalROptions());

        // Default MaxRetries = null → infinite
        for (long i = 0; i < 20; i++)
            policy.NextRetryDelay(RetryContextFactory.Create(i)).Should().NotBeNull();

        // Default MaxDelay = 60s → high attempts are capped
        var highAttempt = policy.NextRetryDelay(RetryContextFactory.Create(100))!.Value;
        highAttempt.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(70)); // 60s + max jitter
    }

    // ── Null guards ───────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        Action act = () => new PollyRetryPolicy(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NegativeBaseDelay_Throws()
    {
        var options = new PollySignalROptions { BaseDelay = TimeSpan.FromSeconds(-1) };
        Action act = () => new PollyRetryPolicy(options);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_InvalidJitterFactor_Throws()
    {
        var options = new PollySignalROptions { JitterFactor = 1.5 };
        Action act = () => new PollyRetryPolicy(options);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
