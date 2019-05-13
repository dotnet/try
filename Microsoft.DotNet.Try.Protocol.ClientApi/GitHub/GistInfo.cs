// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Try.Protocol.ClientApi.GitHub
{
    public class GistInfo : IFeature
    {
        public string Name => nameof(GistInfo);
        [JsonProperty("htmlUrl")]
        public string HtmlUrl { get; }
        [JsonProperty("rawFileUrls")]
        public RawFileUri[] RawFileUris { get;  }

        public GistInfo(string htmlUrl,  IEnumerable<RawFileUri> rawFileUrls)
        {
            if (string.IsNullOrWhiteSpace(htmlUrl))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(htmlUrl));
            }

            if (rawFileUrls == null)
            {
                throw new ArgumentNullException(nameof(rawFileUrls));
            }
            HtmlUrl = htmlUrl;
            RawFileUris = rawFileUrls.Where(f => f!= null).ToArray();

            if (RawFileUris.Length == 0)
            {
                throw new ArgumentException("Collection cannot be empty.", nameof(rawFileUrls));
            }

        }

        public void Apply(FeatureContainer result)
        {
            result.AddProperty("projectOriginType", "gist");
            result.AddProperty("htmlUrl", HtmlUrl);
            result.AddProperty("rawFileUrls", RawFileUris);
        }
    }
}