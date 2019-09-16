// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace MLS.Agent.Telemetry
{
    internal class AllowListToSendFirstArgument : IParseResultLogRule
    {
        public AllowListToSendFirstArgument(
            HashSet<string> topLevelCommandNameAllowList)
        {
            _topLevelCommandNameAllowList = topLevelCommandNameAllowList;
        }

        private HashSet<string> _topLevelCommandNameAllowList { get; }

        public List<ApplicationInsightsEntryFormat> AllowList(ParseResult parseResult)
        {
            var result = new List<ApplicationInsightsEntryFormat>();
            var topLevelCommandNameFromParse = parseResult["try"]?.Tokens.FirstOrDefault()?.Value;
            if (topLevelCommandNameFromParse != null)
            {
                if (_topLevelCommandNameAllowList.Contains(topLevelCommandNameFromParse))
                {
                    result.Add(new ApplicationInsightsEntryFormat(
                        "sublevelparser/command",
                        new Dictionary<string, string>
                        {
                            {"verb", topLevelCommandNameFromParse},
                            {"argument", String.Empty} // No argument for now.
                        }));
                }
            }
            return result;
        }
    }
}
