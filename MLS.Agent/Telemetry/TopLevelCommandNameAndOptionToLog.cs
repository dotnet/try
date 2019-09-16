// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.Collections.Generic;
using System.Linq;

namespace MLS.Agent.Telemetry
{
    internal class TopLevelCommandNameAndOptionToLog : IParseResultLogRule
    {
        public TopLevelCommandNameAndOptionToLog(
            HashSet<string> topLevelCommandName,
            HashSet<string> optionsToLog)
        {
            _topLevelCommandName = topLevelCommandName;
            _optionsToLog = optionsToLog;
        }

        private HashSet<string> _topLevelCommandName { get; }
        private HashSet<string> _optionsToLog { get; }
        private const string TryName = "try";

        public List<ApplicationInsightsEntryFormat> AllowList(ParseResult parseResult)
        {
            var topLevelCommandName = parseResult[TryName]?.Tokens?.FirstOrDefault()?.Value;
            var result = new List<ApplicationInsightsEntryFormat>();
            foreach (var option in _optionsToLog)
            {
                if (_topLevelCommandName.Contains(topLevelCommandName)
                    && parseResult[TryName].ParentCommandResult[topLevelCommandName].Tokens != null
                    && parseResult[TryName].ParentCommandResult[topLevelCommandName].Tokens.Any(x => x.Value == option))
                {
                    Token commandOption =
                        parseResult[TryName].ParentCommandResult[topLevelCommandName].Tokens.First(x => x.Value == option);
                    result.Add(new ApplicationInsightsEntryFormat(
                        "sublevelparser/command",
                        new Dictionary<string, string>
                        {
                            { "verb", topLevelCommandName},
                            {option, commandOption.Value}
                        }));
                }
            }
            return result;
        }
    }
}
