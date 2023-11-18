# Serilog.Sinks.MementoGroup [![Build](https://github.com/aron-albert/serilog-sinks-mementogroup/actions/workflows/build_and_push.yaml/badge.svg?branch=master)](https://github.com/aron-albert/serilog-sinks-mementogroup/actions/workflows/build_and_push.yaml) [![Tests](https://github.com/aron-albert/serilog-sinks-mementogroup/actions/workflows/run_tests.yaml/badge.svg?branch=master)](https://github.com/aron-albert/serilog-sinks-mementogroup/actions/workflows/run_tests.yaml)

If you have ever struggled to find the balance between verbose enough logging and keeping the amount of log entries at minimum, this package can probably help you in that.

## Getting started

To use this sink, first install it from [NuGet](https://www.nuget.org/packages/Serilog.Sinks.MementoGroup/):

```shell
dotnet add package Serilog.Sinks.MementoGroup
```

Then enable the sink using `WriteTo.MementoGroup(...)`:

```csharp
var logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.MementoGroup(cfg =>
        cfg.WriteTo.Console(),
        "CorrelationId")
    .CreateLogger();
```

## Concept

Based on the example fom above, all log entries at or above `Information` level will immediately be forwarded to the inner logger, which is `Console` in this example.

If a log message having a `LogEvent Property` called `CorrelationId` is logged at `Debug` level it is stored in a buffer for 30 seconds. If another log message at or above `Error` level is logged then all the buffered messages belonging to the same group by the `CorrelationId` value gets logged as well to the inner logger, to provide more details about what happened exactly while processing the request.

This way if no error occours the amount of log entries will be less, while in case of an error you will get more logs.

All of the above mentioned values are configurable.

### Examples

Let's imagine that log messages arrive in the order below and the messages with correlation property have the same correlation value.

No error is logged:

| Has correlation property | Log level | Logged |
|---|---|---|
| ✓ | Information | Immediately |
| - | Information | Immediately |
| ✓ | Warning | Immediately |
| - | Warning | Immediately |
| ✓ | Debug | Never |
| - | Debug | Never |
| ✓ | Verbose | Never |
| - | Verbose | Never |

Errr is logged but without correlation property:

| Has correlation property | Log level | Logged |
|---|---|---|
| ✓ | Information | Immediately |
| - | Information | Immediately |
| ✓ | Warning | Immediately |
| - | Warning | Immediately |
| ✓ | Debug | Never |
| - | Debug | Never |
| - | Error | Immediately |
| ✓ | Verbose | Never |
| - | Verbose | Never |

Errr is logged with correlation property:

| Has correlation property | Log level | Logged |
|---|---|---|
| ✓ | Information | Immediately |
| - | Information | Immediately |
| ✓ | Warning | Immediately |
| - | Warning | Immediately |
| ✓ | Debug | When error is logged |
| - | Debug | Never |
| ✓ | Error | Immediately |
| ✓ | Verbose | Never |
| - | Verbose | Never |
| ✓ | Debug | Immediately* |
| - | Debug | Never |

\* because an error for the same correlation group has already been logged

For more examples check out the [example project](https://github.com/aron-albert/serilog-sinks-mementogroup/tree/master/examples/ExampleWebApplication) and the [tests](https://github.com/aron-albert/serilog-sinks-mementogroup/tree/master/test/Serilog.Sinks.MementoGroup.Tests).
