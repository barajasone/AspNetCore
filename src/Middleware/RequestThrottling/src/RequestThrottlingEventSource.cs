// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.RequestThrottling
{
    internal sealed class RequestThrottlingEventSource : EventSource
    {
        public static readonly RequestThrottlingEventSource Log = new RequestThrottlingEventSource();
        private static QueueFrame CachedNonTimerResult = new QueueFrame(null, Log);

        private PollingCounter _rejectedRequestsCounter;
        private PollingCounter _queueLengthCounter;
        private EventCounter _queueDuration;

        private long _rejectedRequests;
        private int _queueLength;

        internal RequestThrottlingEventSource()
            : base("Microsoft.AspNetCore.RequestThrottling")
        {
        }

        // Used for testing
        internal RequestThrottlingEventSource(string eventSourceName)
            : base(eventSourceName)
        {
        }

        [Event(1, Level = EventLevel.Warning)]
        public void RequestRejected()
        {
            Interlocked.Increment(ref _rejectedRequests);
            WriteEvent(1);
        }

        [NonEvent]
        public void QueueSkipped()
        {
            if (IsEnabled())
            {
                _queueDuration.WriteMetric(0);
            }
        }

        [NonEvent]
        public QueueFrame QueueTimer()
        {
            Interlocked.Increment(ref _queueLength);

            if (IsEnabled())
            {
                return new QueueFrame(ValueStopwatch.StartNew(), this);
            }

            return CachedNonTimerResult;
        }

        internal struct QueueFrame : IDisposable
        {
            private ValueStopwatch? _timer;
            private RequestThrottlingEventSource _parent;

            public QueueFrame(ValueStopwatch? timer, RequestThrottlingEventSource parent)
            {
                _timer = timer;
                _parent = parent;
            }

            public void Dispose()
            {
                Interlocked.Decrement(ref _parent._queueLength);

                if (_parent.IsEnabled() && _timer != null)
                {
                    var duration = _timer.Value.GetElapsedTime().TotalMilliseconds;
                    _parent._queueDuration.WriteMetric(duration);
                }
            }
        }

        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            if (command.Command == EventCommand.Enable)
            {
                _rejectedRequestsCounter ??= new PollingCounter("requests-rejected", this, () => _rejectedRequests)
                {
                    DisplayName = "Rejected Requests",
                };

                _queueLengthCounter ??= new PollingCounter("queue-length", this, () => _queueLength)
                {
                    DisplayName = "Queue Length",
                };

                _queueDuration ??= new EventCounter("queue-duration", this)
                {
                    DisplayName = "Average Time in Queue",
                };
            }
        }
    }
}
