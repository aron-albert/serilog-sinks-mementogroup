using Serilog.Core;
using Serilog.Events;
using System.Reflection;

namespace Serilog.Sinks.MementoGroup.Tests;

public class LoggerSinkConfigurationExtensionTests
{
    [Theory]
    [InlineData(LogEventLevel.Verbose)]
    [InlineData(LogEventLevel.Debug)]
    public void SetTargetSinkLogLevelToMinimumLevel(LogEventLevel level)
    {
        var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.MementoGroup(target =>
                target.MinimumLevel.Debug().WriteTo.TestCorrelator(),
                "CorrelationId",
                2000,
                LogEventLevel.Information,
                LogEventLevel.Error,
                level)
            .CreateLogger();

        var sinkFi = logger.GetType().GetField("_sink", BindingFlags.Instance | BindingFlags.NonPublic);
        var sink = sinkFi.GetValue(logger);
        var sinksFi = sink.GetType().GetField("_sinks", BindingFlags.Instance | BindingFlags.NonPublic);
        var sinks = sinksFi.GetValue(sink) as ILogEventSink[];
        var mementoSink = sinks[0];
        var targetLoggerFi = mementoSink.GetType().GetField("_logger", BindingFlags.Instance | BindingFlags.NonPublic);
        var targetLogger = targetLoggerFi.GetValue(mementoSink);
        var minLevelFi = targetLogger.GetType().GetField("_minimumLevel", BindingFlags.Instance | BindingFlags.NonPublic);
        var minimumLevel = minLevelFi.GetValue(targetLogger);

        minimumLevel.Should().Be(level);
    }
}
