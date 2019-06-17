// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Try.Jupyter.Protocol
{
    public abstract class Stream : JupyterMessageContent
    {
        [JsonProperty("name")]
        public string Name { get; }
        [JsonProperty("text")]
        public string Text { get; }

        protected Stream(string name, string text)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }
            Name = name;
            Text = text;
        }
    }
}