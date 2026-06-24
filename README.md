# PollySignalR

[![NuGet](https://img.shields.io/nuget/v/PollySignalR.svg)](https://www.nuget.org/packages/PollySignalR)
[![Downloads](https://img.shields.io/nuget/dt/PollySignalR.svg)](https://www.nuget.org/packages/PollySignalR)
[![CI](https://github.com/Swevo/PollySignalR/actions/workflows/build.yml/badge.svg)](https://github.com/Swevo/PollySignalR/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**Polly v8 reconnect policy for ASP.NET Core SignalR.**  
Drop-in replacement for `WithAutomaticReconnect(TimeSpan[])` with full Polly v8 exponential back-off, jitter, configurable max retries, and max delay. Also provides `StartWithResilienceAsync` for resilient initial connections.

---

## Why PollySignalR?

SignalR's built-in reconnect is limited to a fixed array of delays:

| Feature | `WithAutomaticReconnect(TimeSpan[])` | **PollySignalR** |
|---|:---:|:---:|
| Exponential back-off | ❌ manual | ✅ automatic |
| Jitter (reconnect storm prevention) | ❌ | ✅ |
| Configurable max retries | ❌ array length | ✅ |
| Max delay cap | ❌ last array element | ✅ |
| Linear back-off option | ❌ | ✅ |
| Infinite retries | ❌ | ✅ |
| Polly v8 pipeline for initial connect | ❌ | ✅ |
| Single line of config | ❌ | ✅ |

---

## Installation

```bash
dotnet add package PollySignalR
```

---

## Quick Start

```csharp
var connection = new HubConnectionBuilder()
    .WithUrl("https://example.com/hub")
    .WithPollyReconnect()   // exponential back-off, infinite retries, up to 60 s
    .Build();
```

---

## Configuration

```csharp
var connection = new HubConnectionBuilder()
    .WithUrl("https://example.com/hub")
    .WithPollyReconnect(options =>
    {
        options.MaxRetries   = 10;                          // null = infinite (default)
        options.BaseDelay    = TimeSpan.FromSeconds(1);     // default: 1 s
        options.MaxDelay     = TimeSpan.FromSeconds(60);    // default: 60 s
        options.JitterFactor = 0.5;                         // ±50% noise (default)
        options.BackoffType  = DelayBackoffType.Exponential; // or Linear
    })
    .Build();
```

### How delays are computed

```
delay = clamp(BaseDelay × 2^attempt, 0, MaxDelay)
      + random(−JitterFactor × BaseDelay, +JitterFactor × BaseDelay)
```

| Attempt | BaseDelay=1s, MaxDelay=60s |
|:---:|---|
| 0 | ~1 s |
| 1 | ~2 s |
| 2 | ~4 s |
| 3 | ~8 s |
| 4 | ~16 s |
| 5 | ~32 s |
| 6+ | ~60 s (capped) |

---

## Resilient initial connection

Use `StartWithResilienceAsync` to apply a full Polly v8 pipeline to the _first_ connect attempt (before automatic reconnect takes over):

```csharp
var pipeline = new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions
    {
        ShouldHandle  = new PredicateBuilder().Handle<Exception>(),
        MaxRetryAttempts = 5,
        BackoffType   = DelayBackoffType.Exponential,
        Delay         = TimeSpan.FromSeconds(1),
        UseJitter     = true,
    })
    .AddTimeout(new TimeoutStrategyOptions { Timeout = TimeSpan.FromSeconds(30) })
    .Build();

await connection.StartWithResilienceAsync(pipeline);
```

---

## Blazor WebAssembly

```csharp
// Program.cs
builder.Services.AddSingleton(sp =>
    new HubConnectionBuilder()
        .WithUrl(builder.HostEnvironment.BaseAddress + "chathub")
        .WithPollyReconnect(o =>
        {
            o.BaseDelay  = TimeSpan.FromSeconds(2);
            o.MaxDelay   = TimeSpan.FromSeconds(120);
            o.MaxRetries = null; // never give up
        })
        .Build());
```

---

## Related Packages

| Package | Downloads | Description |
|---|---|---|
| [PollyOpenAI](https://www.nuget.org/packages/PollyOpenAI) | [![Downloads](https://img.shields.io/nuget/dt/PollyOpenAI.svg)](https://www.nuget.org/packages/PollyOpenAI) | Polly v8 resilience for OpenAI and Azure OpenAI API calls |
| [PollyRedis](https://www.nuget.org/packages/PollyRedis) | [![Downloads](https://img.shields.io/nuget/dt/PollyRedis.svg)](https://www.nuget.org/packages/PollyRedis) | Polly v8 resilience for StackExchange.Redis |
| [PollyEFCore](https://www.nuget.org/packages/PollyEFCore) | [![Downloads](https://img.shields.io/nuget/dt/PollyEFCore.svg)](https://www.nuget.org/packages/PollyEFCore) | Polly v8 resilience for EF Core queries and SaveChanges |
| [PollyHealthChecks](https://www.nuget.org/packages/PollyHealthChecks) | [![Downloads](https://img.shields.io/nuget/dt/PollyHealthChecks.svg)](https://www.nuget.org/packages/PollyHealthChecks) | ASP.NET Core health checks for Polly v8 circuit breakers |
| [PollyChaos](https://www.nuget.org/packages/PollyChaos) | [![Downloads](https://img.shields.io/nuget/dt/PollyChaos.svg)](https://www.nuget.org/packages/PollyChaos) | Chaos engineering / fault injection for Polly v8 |
| [PollyMediatR](https://www.nuget.org/packages/PollyMediatR) | [![Downloads](https://img.shields.io/nuget/dt/PollyMediatR.svg)](https://www.nuget.org/packages/PollyMediatR) | Polly v8 pipelines for MediatR request handlers |
| [PollyElasticsearch](https://github.com/Swevo/PollyElasticsearch) | Polly v8 for Elastic.Clients.Elasticsearch |
| [PollyAzureKeyVault](https://github.com/Swevo/PollyAzureKeyVault) | Polly v8 for Azure Key Vault |
| [PollyBackoff](https://www.nuget.org/packages/PollyBackoff) | [![Downloads](https://img.shields.io/nuget/dt/PollyBackoff.svg)](https://www.nuget.org/packages/PollyBackoff) | Custom back-off strategies for Polly v8 |

---

| [PollyGrpc](https://www.nuget.org/packages/PollyGrpc) | Polly v8 resilience (retry, CB, timeout) for gRPC .NET clients via Interceptor |
| [PollyKafka](https://www.nuget.org/packages/PollyKafka) | Polly v8 resilience (retry, CB, timeout) for Confluent.Kafka producers and consumers |
| [PollyAzureServiceBus](https://www.nuget.org/packages/PollyAzureServiceBus) | Polly v8 resilience (retry, CB, timeout) for Azure Service Bus senders and receivers |
## License

MIT
