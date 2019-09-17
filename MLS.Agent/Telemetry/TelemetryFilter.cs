// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System;
using System.Linq;
using System.CommandLine;
using MLS.Agent.Telemetry.Utils;

namespace MLS.Agent.Telemetry
{
    public class TelemetryFilter : ITelemetryFilter
    {
        private const string TryName = "try";
        private readonly Func<string, string> _hash;

        public TelemetryFilter(Func<string, string> hash)
        {
            _hash = hash ?? throw new ArgumentNullException(nameof(hash));
        }

        public IEnumerable<ApplicationInsightsEntryFormat> Filter(object objectToFilter)
        {
            var result = new List<ApplicationInsightsEntryFormat>();

            if (objectToFilter is ParseResult parseResult)
            {
                var topLevelCommandName = parseResult[TryName]?.Tokens.FirstOrDefault()?.Value;
                if (topLevelCommandName != null)
                {
                    foreach (IParseResultLogRule rule in ParseResultLogRules)
                    {
                        result.AddRange(rule.AllowList(parseResult));
                    }
                }
            }
            else if(objectToFilter is CommandResult topLevelCommandParserResult)
            {
                result.Add(new ApplicationInsightsEntryFormat(
                            "toplevelparser/command",
                            new Dictionary<string, string>()
                        {{ "verb", topLevelCommandParserResult.Command.Name }}
                ));

            }

            return result.Select(r => r.WithAppliedToPropertiesValue(_hash)).ToList();
        }

        private static List<IParseResultLogRule> ParseResultLogRules => new List<IParseResultLogRule>
        {
            new AllowListToSendFirstArgument(new HashSet<string> {"fsi"})
        };
    }
}
