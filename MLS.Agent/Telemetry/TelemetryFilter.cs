// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System;
using System.Linq;
using System.CommandLine;
using MLS.Agent.Telemetry.Utils;
using System.Collections;

namespace MLS.Agent.Telemetry
{
    public class TelemetryFilter : ITelemetryFilter
    {
        private const string DotNetTryName = "dotnet-try";
        private readonly Func<string, string> _hash;

        public TelemetryFilter(Func<string, string> hash)
        {
            _hash = hash ?? throw new ArgumentNullException(nameof(hash));
        }

        public IEnumerable<ApplicationInsightsEntryFormat> Filter(object objectToFilter)
        {
            if (objectToFilter == null)
            {
                return new List<ApplicationInsightsEntryFormat>();
            }

            var result = new List<ApplicationInsightsEntryFormat>();

            if (objectToFilter is ParseResult parseResult)
            {
                var isDotNetTryCommand = parseResult.RootCommandResult?.Token.Value == DotNetTryName;
                var commandName = parseResult.Tokens?.Take(1).Select(x => x.Value).FirstOrDefault();

                if (isDotNetTryCommand && commandName == parseResult.CommandResult?.Command?.Name)
                {
                    var arguments = parseResult.CommandResult.Tokens.Skip(1).Select(x => x.Value);
                    if (TryFindSuccessfulRule(commandName, arguments, out var rule))
                    {
                        result.Add(CreateEntry(rule));
                    }
                }
            }

            return result.Select(r => r.WithAppliedToPropertiesValue(_hash)).ToList();
        }

        private bool TryFindSuccessfulRule(string commandName, IEnumerable<string> arguments, out SuccessfulRule outRule)
        {
            var rule = 
                ParseResultMatchingRules
                .FirstOrDefault(x => x.Command == commandName && x.Arguments.SequenceEqual(arguments));

            if (rule == null)
            {
                outRule = null;
                return false;
            }
            else
            {
                outRule = new SuccessfulRule(rule.Command, rule.Arguments);
                return true;
            }
        }

        // TODO: Handle arguments and options.
        private ApplicationInsightsEntryFormat CreateEntry(SuccessfulRule rule)
        {
            if (rule.Arguments.Count() == 0)
            {
                return new ApplicationInsightsEntryFormat(
                    "toplevelparser/command",
                    new Dictionary<string, string>
                    {
                        { "verb", rule.Command }
                    });
            }
            else
            {
                return new ApplicationInsightsEntryFormat(
                    "toplevelparser/command",
                    new Dictionary<string, string>
                    {
                        { "verb", rule.Command },
                    });
            }
        }

        private class SuccessfulRule
        {
            public SuccessfulRule(string command, IEnumerable<string> arguments)
            {
                Command = command;
                Arguments = arguments;
            }

            public string Command { get; }
            public IEnumerable<string> Arguments { get; }
        }

        private class FilterRule
        {
            public FilterRule(string command, IEnumerable<string> arguments)
            {
                Command = command;
                Arguments = arguments;
            }

            public string Command { get; }
            public IEnumerable<string> Arguments { get; }
        }

        private static FilterRule[] ParseResultMatchingRules => new FilterRule[]
        {
            new FilterRule("jupyter", new string[]{})
        };
    }
}
