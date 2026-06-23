namespace PollySignalR.Tests;

public class HubConnectionExtensionsTests
{
    // ── WithPollyReconnect ─────────────────────────────────────────────────

    [Fact]
    public void WithPollyReconnect_NullBuilder_Throws()
    {
        IHubConnectionBuilder builder = null!;
        Action act = () => builder.WithPollyReconnect();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithPollyReconnect_DefaultOptions_DoesNotThrow()
    {
        var builder = new HubConnectionBuilder().WithUrl("http://localhost/hub");
        Action act = () => builder.WithPollyReconnect();
        act.Should().NotThrow();
    }

    [Fact]
    public void WithPollyReconnect_WithOptions_DoesNotThrow()
    {
        var builder = new HubConnectionBuilder().WithUrl("http://localhost/hub");
        Action act = () => builder.WithPollyReconnect(o =>
        {
            o.MaxRetries = 5;
            o.BaseDelay = TimeSpan.FromSeconds(2);
            o.MaxDelay = TimeSpan.FromSeconds(30);
        });
        act.Should().NotThrow();
    }

    [Fact]
    public void WithPollyReconnect_ReturnsBuilder_ForChaining()
    {
        var builder = new HubConnectionBuilder().WithUrl("http://localhost/hub");
        var result = builder.WithPollyReconnect();
        result.Should().BeSameAs(builder);
    }

    // ── StartWithResilienceAsync ───────────────────────────────────────────

    [Fact]
    public async Task StartWithResilienceAsync_NullConnection_Throws()
    {
        HubConnection connection = null!;
        var act = () => connection.StartWithResilienceAsync(ResiliencePipeline.Empty);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task StartWithResilienceAsync_NullPipeline_Throws()
    {
        var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/hub")
            .Build();
        var act = () => connection.StartWithResilienceAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
