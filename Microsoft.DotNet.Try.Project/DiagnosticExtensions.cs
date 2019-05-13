// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Try.Protocol;
using RoslynDiagnosticSeverity = Microsoft.CodeAnalysis.DiagnosticSeverity;
using TdnDiagnosticSeverity = Microsoft.DotNet.Try.Protocol.DiagnosticSeverity;

namespace Microsoft.DotNet.Try.Project
{
    public static class DiagnosticExtensions
    {
        public static TdnDiagnosticSeverity ConvertSeverity(this Diagnostic diagnostic)
        {
            return (TdnDiagnosticSeverity) diagnostic.Severity;
        }

        public static SerializableDiagnostic ToSerializableDiagnostic(
            this Diagnostic diagnostic,
            string message = null,
            BufferId bufferId = null)
        {
            var diagnosticMessage = diagnostic.GetMessage();

            var startPosition = diagnostic.Location.GetLineSpan().Span.Start;

            var location =
                diagnostic.Location != null
                    ? $"{diagnostic.Location.SourceTree?.FilePath}({startPosition.Line + 1},{startPosition.Character + 1}): {GetMessagePrefix()}"
                    : null;

            return new SerializableDiagnostic(diagnostic.Location?.SourceSpan.Start ?? throw new ArgumentException(nameof(diagnostic.Location)),
                                              diagnostic.Location.SourceSpan.End,
                                              message ?? diagnosticMessage,
                                              diagnostic.ConvertSeverity(),
                                              diagnostic.Descriptor.Id,
                                              bufferId,
                                              location);

            string GetMessagePrefix()
            {
                string prefix;
                switch (diagnostic.Severity)
                {
                    case RoslynDiagnosticSeverity.Hidden:
                        prefix = "hidden";
                        break;
                    case RoslynDiagnosticSeverity.Info:
                        prefix = "info";
                        break;
                    case RoslynDiagnosticSeverity.Warning:
                        prefix = "warning";
                        break;
                    case RoslynDiagnosticSeverity.Error:
                        prefix = "error";
                        break;
                    default:
                        return null;
                }

                return $"{prefix} {diagnostic.Id}";
            }
        }
    }
}
