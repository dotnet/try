// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MLS.Agent.Controllers
{
    public static class DisplayExtensions
    {
        public static async Task<string[]> ToDisplayString(this HttpResponseMessage response)
        {
            var sb = new StringBuilder();

            sb.Append("Status code: ");
            sb.Append((int) response.StatusCode);
            sb.Append(" ");
            sb.Append(response.ReasonPhrase);
            sb.AppendLine();

            sb.AppendLine("Content headers:");

            foreach (var header in response.Headers.Concat(response.Content.Headers))
            {
                sb.Append("  ");
                sb.Append(header.Key);
                sb.Append(": ");
                sb.Append(string.Join("; ", header.Value));
                sb.AppendLine();
            }

            sb.AppendLine("Content:");

            var content = await response.Content.ReadAsStringAsync();

            try
            {
                var json = JToken.Parse(content);

                sb.Append(json.ToString(Formatting.Indented));

                sb.Replace("\r\n", "\n");
            }
            catch (JsonReaderException)
            {
               sb.Append(content);
            }

            return sb.ToString().Split("\n");
        }
    }
}
