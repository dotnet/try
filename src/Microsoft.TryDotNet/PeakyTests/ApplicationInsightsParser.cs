// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Newtonsoft.Json.Linq;

namespace Microsoft.TryDotNet.PeakyTests;

public static class ApplicationInsightsParser
{
    public static IEnumerable<JObject> ExtractEntries(JToken aiQueryResult, string tableName = "PrimaryResult")
    {
        var columns = aiQueryResult.SelectTokens($"$..tables[?(@.name == '{tableName}')].columns").Values<JToken>().ToArray();
        var rows = aiQueryResult.SelectTokens($"$..tables[?(@.name == '{tableName}')].rows").Values<JToken>().ToArray();

        var logs = ImmutableArray.CreateBuilder<JObject>();
        foreach (var row in rows)
        {
            var log = new JObject();
            var i = 0;
            foreach (var column in columns)
            {
                var columnName = column["name"].ToString();
                if (column["type"].ToString() == "dynamic")
                {
                    log[columnName] = JObject.Parse(row[i].ToString());
                }
                else
                {
                    log[columnName] = row[i];
                }
                i++;
            }
            logs.Add(log);
        }
        return logs.ToImmutableArray();
    }
}