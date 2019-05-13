// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Try.Protocol.ClientApi
{
    public class RawFileUri
    {
        [JsonProperty("fileName")]
        public string FileName { get;  }
        [JsonProperty("url")]
        public string Url { get; }

        public RawFileUri(string fileName, string url)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(fileName));
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(url));
            }

            FileName = fileName;
            Url = url;
        }
    }
}