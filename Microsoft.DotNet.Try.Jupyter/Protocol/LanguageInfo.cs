// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Try.Jupyter.Protocol
{
    public class LanguageInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("mimetype")]
        public string MimeType { get; set; }

        [JsonProperty("file_extension")]
        public string FileExtension { get; set; }

        [JsonProperty("pygments_lexer")]
        public string PygmentsLexer { get; set; }

        [JsonProperty("codemirror_mode", NullValueHandling = NullValueHandling.Ignore)]
        public object CodeMirrorMode { get; set; }

        [JsonProperty("nbconvert_exporter", NullValueHandling = NullValueHandling.Ignore)]
        public string NbConvertExporter { get; set; }

        [JsonProperty("help_links")]
        public List<Link> HelpLinks { get;  } = new List<Link>();
    }
}