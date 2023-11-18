using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.MementoGroup.Tests;

public class MementoGroupSinkTests
{
    internal static readonly string CorrelationPropertyName = "CorrelationId";
    private readonly Logger _targetLogger;
    private readonly MementoGroupSink _sink;

    public MementoGroupSinkTests()
    {
        _targetLogger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.TestCorrelator()
            .CreateLogger();

        _sink = new(_targetLogger, CorrelationPropertyName, TimeSpan.FromSeconds(10), LogEventLevel.Information, LogEventLevel.Error, LogEventLevel.Debug);
    }

    [Theory]
    [InlineData(LogEventLevel.Verbose)]
    [InlineData(LogEventLevel.Debug)]
    public void Log_WithoutCorrelationPropertyBelowPassthrough_NotLogToTarget(LogEventLevel level)
    {
        using (TestCorrelator.TestCorrelator.CreateContext())
        {
            _sink.Emit(CreateLogEvent(level, false));

            TestCorrelator.TestCorrelator.GetLogEventsFromCurrentContext().Should().BeEmpty();
        }
    }

    [Theory]
    [InlineData(LogEventLevel.Information)]
    [InlineData(LogEventLevel.Warning)]
    [InlineData(LogEventLevel.Error)]
    [InlineData(LogEventLevel.Fatal)]
    public void Log_WithoutCorrelationPropertyAtOrAbovePassthrough_LogToTarget(LogEventLevel level)
    {
        using (TestCorrelator.TestCorrelator.CreateContext())
        {
            _sink.Emit(CreateLogEvent(level, false));

            TestCorrelator.TestCorrelator.GetLogEventsFromCurrentContext()
                .Should().ContainSingle()
                .Which.Level.Should().Be(level);
        }
    }

    [Theory]
    [InlineData(LogEventLevel.Verbose)]
    [InlineData(LogEventLevel.Debug)]
    public void Log_WithCorrelationPropertyBelowPassthrough_NotLogToTarget(LogEventLevel level)
    {
        using (TestCorrelator.TestCorrelator.CreateContext())
        {
            _sink.Emit(CreateLogEvent(level));

            TestCorrelator.TestCorrelator.GetLogEventsFromCurrentContext().Should().BeEmpty();
        }
    }

    [Theory]
    [InlineData(LogEventLevel.Information)]
    [InlineData(LogEventLevel.Warning)]
    [InlineData(LogEventLevel.Error)]
    [InlineData(LogEventLevel.Fatal)]
    public void Log_WithCorrelationPropertyAtOrAbovePassthrough_LogToTarget(LogEventLevel level)
    {
        using (TestCorrelator.TestCorrelator.CreateContext())
        {
            _sink.Emit(CreateLogEvent(level));

            TestCorrelator.TestCorrelator.GetLogEventsFromCurrentContext()
                .Should().ContainSingle()
                .Which.Level.Should().Be(level);
        }
    }

    [Fact]
    public void Log_ManyWithoutCorrelationPropertyThenTrigger_LogOnlyAbovePassthroughToTarget()
    {
        using (TestCorrelator.TestCorrelator.CreateContext())
        {
            _sink.Emit(CreateLogEvent(LogEventLevel.Verbose, false));
            _sink.Emit(CreateLogEvent(LogEventLevel.Debug, false));
            _sink.Emit(CreateLogEvent(LogEventLevel.Information, false));
            _sink.Emit(CreateLogEvent(LogEventLevel.Warning, false));
            _sink.Emit(CreateLogEvent(LogEventLevel.Error, false));
            _sink.Emit(CreateLogEvent(LogEventLevel.Fatal, false));

            TestCorrelator.TestCorrelator.GetLogEventsFromCurrentContext()
                .Should().HaveCount(4)
                .And.Contain(e => e.Level >= LogEventLevel.Information);
        }
    }

    [Fact]
    public void Log_ManyWithCorrelationPropertyThenTrigger_LogAllAboveMinimumToTarget()
    {
        using (TestCorrelator.TestCorrelator.CreateContext())
        {
            _sink.Emit(CreateLogEvent(LogEventLevel.Verbose));
            _sink.Emit(CreateLogEvent(LogEventLevel.Debug));
            _sink.Emit(CreateLogEvent(LogEventLevel.Information));
            _sink.Emit(CreateLogEvent(LogEventLevel.Warning));
            _sink.Emit(CreateLogEvent(LogEventLevel.Error));
            _sink.Emit(CreateLogEvent(LogEventLevel.Fatal));

            TestCorrelator.TestCorrelator.GetLogEventsFromCurrentContext()
                .Should().HaveCount(5)
                .And.Contain(e => e.Level >= LogEventLevel.Debug);
        }
    }

    [Fact]
    public void Log_ManyWithoutCorrelationPropertyThenTriggerAndLogMore_LogOnlyAbovePassthroughToTarget()
    {
        using (TestCorrelator.TestCorrelator.CreateContext())
        {
            for (int i = 0; i < 2; i++)
            {
                _sink.Emit(CreateLogEvent(LogEventLevel.Verbose, false));
                _sink.Emit(CreateLogEvent(LogEventLevel.Debug, false));
                _sink.Emit(CreateLogEvent(LogEventLevel.Information, false));
                _sink.Emit(CreateLogEvent(LogEventLevel.Warning, false));
                _sink.Emit(CreateLogEvent(LogEventLevel.Error, false));
                _sink.Emit(CreateLogEvent(LogEventLevel.Fatal, false));
            }

            TestCorrelator.TestCorrelator.GetLogEventsFromCurrentContext()
                .Should().HaveCount(8)
                .And.Contain(e => e.Level >= LogEventLevel.Information);
        }
    }

    [Fact]
    public void Log_ManyWithCorrelationPropertyThenTriggerAndLogMore_LogAllAboveMinimumToTarget()
    {
        using (TestCorrelator.TestCorrelator.CreateContext())
        {
            _sink.Emit(CreateLogEvent(LogEventLevel.Verbose));
            _sink.Emit(CreateLogEvent(LogEventLevel.Debug));
            _sink.Emit(CreateLogEvent(LogEventLevel.Information));
            _sink.Emit(CreateLogEvent(LogEventLevel.Warning));
            _sink.Emit(CreateLogEvent(LogEventLevel.Error));
            _sink.Emit(CreateLogEvent(LogEventLevel.Fatal));
            _sink.Emit(CreateLogEvent(LogEventLevel.Verbose));
            _sink.Emit(CreateLogEvent(LogEventLevel.Debug));
            _sink.Emit(CreateLogEvent(LogEventLevel.Information));
            _sink.Emit(CreateLogEvent(LogEventLevel.Verbose));
            _sink.Emit(CreateLogEvent(LogEventLevel.Debug));
            _sink.Emit(CreateLogEvent(LogEventLevel.Warning));
            _sink.Emit(CreateLogEvent(LogEventLevel.Verbose));
            _sink.Emit(CreateLogEvent(LogEventLevel.Debug));

            TestCorrelator.TestCorrelator.GetLogEventsFromCurrentContext()
                .Should().HaveCount(10)
                .And.Contain(e => e.Level >= LogEventLevel.Debug);
        }
    }

    [Fact]
    public async Task Log_ManyWithCorrelationPropertyConcurrentlyBelowTrigger_LogOnlyAbovePassthroughToTarget()
    {
        using (TestCorrelator.TestCorrelator.CreateContext())
        {
            var tasks = Enumerable.Range(0, 1000000).Select(i => new Task(() => _sink.Emit(CreateLogEvent((LogEventLevel)(i % 4))))).ToList();
            Parallel.ForEach(tasks, t => t.Start());

            await Task.WhenAll(tasks);

            TestCorrelator.TestCorrelator.GetLogEventsFromCurrentContext()
                .Should().HaveCount(500000)
                .And.Contain(e => e.Level >= LogEventLevel.Information);
        }
    }

    [Fact]
    public async Task Log_ManyWithCorrelationPropertyConcurrentlyThenTriggerAndLogMore_LogAllAboveMinimumToTarget()
    {
        using (TestCorrelator.TestCorrelator.CreateContext())
        {
            var tasks = Enumerable.Range(0, 1200000).Select(i => new Task(() => _sink.Emit(CreateLogEvent((LogEventLevel)(i % 6))))).ToList();
            Parallel.ForEach(tasks, t => t.Start());

            await Task.WhenAll(tasks);

            TestCorrelator.TestCorrelator.GetLogEventsFromCurrentContext()
                .Should().HaveCount(1000000)
                .And.Contain(e => e.Level >= LogEventLevel.Information);
        }
    }

    private static LogEvent CreateLogEvent(LogEventLevel level, bool hasCorrelationProperty = true) =>
        new LogEvent(DateTimeOffset.Now,
            level,
            null,
            new MessageTemplate($"Test {level} message",
                Enumerable.Empty<Parsing.MessageTemplateToken>()),
            hasCorrelationProperty ?
                new List<LogEventProperty> { new LogEventProperty(CorrelationPropertyName, new ScalarValue("correlation-id value")) } :
                Enumerable.Empty<LogEventProperty>());
}
