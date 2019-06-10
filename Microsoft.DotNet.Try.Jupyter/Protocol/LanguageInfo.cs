// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    }

    public class CSharpLanguageInfo : LanguageInfo
    {
        public CSharpLanguageInfo(string version = "7.3")
        {
            Name = "C#";
            Version = version;
            MimeType = "text/x-csharp";
            FileExtension = ".cs";
            PygmentsLexer = "c#";
        }
    }

    public class FSharpLanguageInfo : LanguageInfo
    {
        public FSharpLanguageInfo(string version = "4.5")
        {
            Name = "F#";
            Version = version;
            MimeType = "text/x-fsharp";
            FileExtension = ".fs";
            PygmentsLexer = "fsharp";
        }
    }

    public class VBnetLanguageInfo : LanguageInfo
    {
        public VBnetLanguageInfo(string version = "15.0"){
            Name = "VB.Net";
            Version = version;
            MimeType = "text/x-vbnet";
            FileExtension = ".vb";
            PygmentsLexer = "vbnet";
        }
    }
}