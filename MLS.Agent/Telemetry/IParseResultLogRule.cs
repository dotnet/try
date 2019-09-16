// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.Collections.Generic;

namespace MLS.Agent.Telemetry
{
    internal interface IParseResultLogRule
    {
        List<ApplicationInsightsEntryFormat> AllowList(ParseResult parseResult);
    }
}
