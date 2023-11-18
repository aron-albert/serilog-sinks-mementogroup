using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Serilog.Sinks.MementoGroup
{
    internal class MementoGroupSink : ILogEventSink, IDisposable
    {
        private readonly Logger _logger;
        private readonly string _correlationProperty;
        private readonly TimeSpan _bufferExpiry;
        private readonly LogEventLevel _passthroughLevel;
        private readonly LogEventLevel _triggerLevel;
        private readonly LogEventLevel _minimumLevel;
        private readonly ConcurrentDictionary<string, LogEntryBuffer> _logBuffers;
        private readonly CancellationTokenSource _cts;

        public MementoGroupSink(Logger logger,
            string correlationProperty,
            TimeSpan bufferExpiry,
            LogEventLevel passthroughLevel,
            LogEventLevel triggerLevel,
            LogEventLevel minimumLevel)
        {
            _logger = logger;
            _correlationProperty = correlationProperty;
            _bufferExpiry = bufferExpiry;
            _passthroughLevel = passthroughLevel;
            _triggerLevel = triggerLevel;
            _minimumLevel = minimumLevel;

            _logBuffers = new ConcurrentDictionary<string, LogEntryBuffer>();
            _cts = new CancellationTokenSource();

            Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(_bufferExpiry, _cts.Token);

                    var expiryTime = DateTimeOffset.Now - _bufferExpiry;
                    var expiredBuffers = _logBuffers.Where(l => l.Value.LastMessageDateTime < expiryTime);

                    foreach (var expiredBuffer in expiredBuffers)
                    {
                        _logBuffers.TryRemove(expiredBuffer.Key, out _);
                    }
                }
            }, _cts.Token);
        }

        public void Emit(LogEvent logEvent)
        {
            var correlationValue = logEvent.Properties.TryGetValue(_correlationProperty, out var correlationPropertyValue) ?
                correlationPropertyValue.ToString() : null;

            if (correlationValue != null)
            {
                var buffer = _logBuffers.GetOrAdd(correlationValue,
                    _ => new LogEntryBuffer(correlationValue, _logger, _passthroughLevel, _triggerLevel, _minimumLevel));

                buffer.Write(logEvent);
            }
            else if (logEvent.Level >= _passthroughLevel)
            {
                _logger.Write(logEvent);
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
        }
    }
}
