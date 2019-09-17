// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MLS.Agent.Telemetry;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MLS.Agent.Tests
{
    public class FakeRecordEventNameTelemetry : ITelemetry
    {
        public bool Enabled { get; set; }

        public string EventName { get; set; }

        public void TrackEvent(string eventName,
            IDictionary<string, string> properties,
            IDictionary<string, double> measurements)
        {
            LogEntries.Add(
                new LogEntry
                {
                    EventName = eventName,
                    Measurement = measurements,
                    Properties = properties
                });
        }

        public ConcurrentBag<LogEntry> LogEntries { get; set; } = new ConcurrentBag<LogEntry>();

        public class LogEntry
        {
            public string EventName { get; set; }
            public IDictionary<string, string> Properties { get; set; }
            public IDictionary<string, double> Measurement { get; set; }
        }
    }
}
