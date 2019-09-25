// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class CompleteRequestHandler : RequestHandlerBase<CompleteRequest>
    {
        private static readonly Regex _lastToken = new Regex(@"(?<lastToken>\S+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline);


        public CompleteRequestHandler(IKernel kernel, IScheduler scheduler = null)
            : base(kernel, scheduler ?? CurrentThreadScheduler.Instance)
        {
        }

        public async Task Handle(JupyterRequestContext context)
        {
            var completeRequest = GetJupyterRequest(context);

            var command = new RequestCompletion(completeRequest.Code, completeRequest.CursorPosition);

            await SendAsync(context, command);
        }

        protected override void OnKernelEventReceived(
            IKernelEvent @event, 
            JupyterRequestContext context)
        {
            switch (@event)
            {
                case CompletionRequestCompleted completionRequestCompleted:
                    OnCompletionRequestCompleted(
                        completionRequestCompleted, 
                        context.Request, 
                        context.ServerChannel);
                    break;
            }
        }

        private static void OnCompletionRequestCompleted(CompletionRequestCompleted completionRequestCompleted, Message request, IReplyChannel serverChannel)
        {
            var command = completionRequestCompleted.Command as RequestCompletion;

            var pos = ComputeReplacementStartPosition(command.Code, command.CursorPosition);
            var reply = new CompleteReply(pos, command.CursorPosition, matches: completionRequestCompleted.CompletionList.Select(e => e.InsertText).ToList());

            serverChannel.Send(reply, request);
        }

        private static int ComputeReplacementStartPosition(string code, int cursorPosition)
        {
            var pos = cursorPosition;

            if (pos > 0)
            {
                var codeToCursor = code.Substring(0, pos);
                var match = _lastToken.Match(codeToCursor);
                if (match.Success)
                {
                    var token = match.Groups["lastToken"];
                    if (token.Success)
                    {
                        var lastDotPosition = token.Value.LastIndexOf(".", StringComparison.InvariantCultureIgnoreCase);
                        if (lastDotPosition >= 0)
                        {
                            pos = token.Index + lastDotPosition + 1;
                        }
                        else
                        {
                            pos = token.Index;
                        }
                    }
                }

            }

            return pos;
        }
    }
}