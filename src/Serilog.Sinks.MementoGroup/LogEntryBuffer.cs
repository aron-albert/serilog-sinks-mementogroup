using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Serilog.Sinks.MementoGroup
{
    internal class LogEntryBuffer
    {
        private readonly Logger _logger;
        private readonly LogEventLevel _passthroughLevel;
        private readonly LogEventLevel _triggerLevel;
        private readonly LogEventLevel _minimumLevel;
        private ConcurrentQueue<LogEvent>? _logEventsQueue;
        private DateTimeOffset _lastMessageDateTime;

        public LogEntryBuffer(string key, Logger logger, LogEventLevel passthroughLevel, LogEventLevel triggerLevel, LogEventLevel minimumLevel)
        {
            Key = key;

            _logger = logger;
            _passthroughLevel = passthroughLevel;
            _triggerLevel = triggerLevel;
            _minimumLevel = minimumLevel;

            _logEventsQueue = new ConcurrentQueue<LogEvent>();
            _lastMessageDateTime = DateTimeOffset.Now;
        }

        public string Key { get; }

        public DateTimeOffset LastMessageDateTime { get => _lastMessageDateTime; }

        public void Write(LogEvent logEvent)
        {
            _lastMessageDateTime = logEvent.Timestamp;

            if (logEvent.Level >= _triggerLevel)
            {
                Flush();
            }

            if (logEvent.Level >= _passthroughLevel)
            {
                _logger.Write(logEvent);
            }
            else if (logEvent.Level >= _minimumLevel)
            {
                // TODO: find better way of concurrency handling
                _logEventsQueue?.Enqueue(logEvent);
                if (_logEventsQueue == null)
                {
                    _logger.Write(logEvent);
                }
            }
        }

        private void Flush()
        {
            var queue = Interlocked.Exchange(ref _logEventsQueue, null);
            while (queue?.TryDequeue(out var queuedLogEvent) ?? false)
            {
                _logger.Write(queuedLogEvent);
            }
        }
    }
}
