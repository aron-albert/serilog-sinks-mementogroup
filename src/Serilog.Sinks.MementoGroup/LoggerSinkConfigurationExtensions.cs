using Serilog.Configuration;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.MementoGroup;
using System;

namespace Serilog
{
    public static class LoggerSinkConfigurationExtensions
    {
        public const long DefaultBufferExpiryInMs = 30000;

        /// <summary>
        /// Store log entries whose level is below the <see cref="passthroughLevel"/> and above or equal to <see cref="minimumLevel"/> 
        /// in groups by the given <see cref="correlationProperty"/> in the <see cref="LogEvent.Properties"/>.
        /// If a <see cref="LogEvent"/> with level higher than or equal to <see cref="triggerLevel"/> arrives then the stored 
        /// <see cref="LogEvent"/>s are flushed to the target logger providing more details for debugging.
        /// </summary>
        /// <param name="sinkConfiguration"></param>
        /// <param name="configureTargetLogger">Configuration action of the target logger that will be used for actual logging.</param>
        /// <param name="correlationProperty">Key of the property which is used to group the log entries (e.g. CorrelationId).</param>
        /// <param name="bufferExpiryInMs">The duration to keep the buffer alive after the last log message.</param>
        /// <param name="passthroughLevel">Log entries at or above this level will immediately be transferred to the target logger.</param>
        /// <param name="triggerLevel">If a log entry at or above this level arrives messages from the buffer will be flushed to the target logger.</param>
        /// <param name="minimumLevel">The lowest log level that are saved to the buffer.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static LoggerConfiguration MementoGroup(this LoggerSinkConfiguration sinkConfiguration,
            Action<LoggerConfiguration> configureTargetLogger,
            string correlationProperty,
            long bufferExpiryInMs = DefaultBufferExpiryInMs,
            LogEventLevel passthroughLevel = LogEventLevel.Information,
            LogEventLevel triggerLevel = LogEventLevel.Error,
            LogEventLevel minimumLevel = LogEventLevel.Debug)
        {
            if (configureTargetLogger == null)
            {
                throw new ArgumentNullException(nameof(configureTargetLogger));
            }
            if (string.IsNullOrWhiteSpace(correlationProperty))
            {
                throw new ArgumentNullException(nameof(correlationProperty));
            }
            if (bufferExpiryInMs < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferExpiryInMs));
            }

            if (passthroughLevel <= minimumLevel)
            {
                SelfLog.WriteLine("Passthrough is not higher than minimum level which means all entries above the minimum level will be logged.");
            }
            if (triggerLevel <= passthroughLevel)
            {
                SelfLog.WriteLine("Trigger is not higher than passthrough level which means all entries above the minimum level will be logged.");
            }

            var loggerConfiguration = new LoggerConfiguration();

            configureTargetLogger(loggerConfiguration);

            var logger = loggerConfiguration.MinimumLevel.Is(minimumLevel).CreateLogger();

            var sink = new MementoGroupSink(logger, correlationProperty, TimeSpan.FromMilliseconds(bufferExpiryInMs), passthroughLevel, triggerLevel, minimumLevel);

            return sinkConfiguration.Sink(sink);
        }
    }
}
