// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace WorkspaceServer.Kernel
{
    public interface IKernelEvent
    {
    }

    public interface IKernelCommand
    {
    }

    public class Started : IKernelEvent
    {
    }

    public class Stopped : IKernelEvent
    {
    }

    public class StandardOutputReceived : IKernelEvent
    {
        public string Content { get; }

        public StandardOutputReceived(string content)
        {
            Content = content;
        }
    }

    public class StandardErrorReceived : IKernelEvent
    {
        public string Content { get; }

        public StandardErrorReceived(string content)
        {
            Content = content;
        }
    }

    public class StandardInputReceived : IKernelEvent
    {
        public string Content { get; }

        public StandardInputReceived(string content)
        {
            Content = content;
        }
    }

    public class PackageAdded : IKernelEvent
    {
    }

    public class DiagnosticsReceived : IKernelEvent
    {
    }

    public class CompletionReceived : IKernelEvent
    {
    }

    public class SignatureHelpReceived : IKernelEvent
    {
    }

    public class DocumentationReceived : IKernelEvent
    {
    }

    public class SendStandardInput : IKernelCommand
    {
    }


    /// <summary>
    /// add a packages to the execution
    /// </summary>
    public class AddPackage : IKernelCommand
    {
    }

    public class SubmitCode : IKernelCommand
    {
        public string Value { get; }

        public SubmitCode(string value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }

    public class RequestCompletion : IKernelCommand
    {
    }

    public class RequestDiagnostics : IKernelCommand
    {
    }

    public class RequestSignatureHelp : IKernelCommand
    {
    }

    public class RequestDocumentation : IKernelCommand
    {
    }

}