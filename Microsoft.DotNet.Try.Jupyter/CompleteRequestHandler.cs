// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.DotNet.Try.Jupyter.Protocol;
using WorkspaceServer.Kernel;

namespace Microsoft.DotNet.Try.Jupyter
{
    public class CompleteRequestHandler: RequestHandlerBase<CompleteRequest>
    {
        private static readonly Regex _lastToken = new Regex(@"(?<lastToken>\S+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline);


        public CompleteRequestHandler(IKernel kernel) : base(kernel)
        {
            
        }

        public async Task Handle(JupyterRequestContext context)
        {
            var completeRequest = GetRequest(context);

            context.RequestHandlerStatus.SetAsBusy();

            var command = new RequestCompletion(completeRequest.Code, completeRequest.CursorPosition);

            var openRequest = new OpenRequest(context, completeRequest, 0, null);

            OpenRequests[command] = openRequest;

            var kernelResult = await Kernel.SendAsync(command);
            openRequest.AddDisposable(kernelResult.KernelEvents.Subscribe(OnKernelResultEvent));

        }

        void OnKernelResultEvent(IKernelEvent value)
        {
            switch (value)
            {

                case CompletionRequestCompleted completionRequestCompleted:
                    OnCompletionRequestCompleted(completionRequestCompleted, OpenRequests);
                    break;
                case CompletionRequestReceived _:
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private static void OnCompletionRequestCompleted(CompletionRequestCompleted completionRequestCompleted, ConcurrentDictionary<IKernelCommand, OpenRequest> openRequests)
        {
            openRequests.TryGetValue(completionRequestCompleted.Command, out var openRequest);
            if (openRequest == null)
            {
                return;
            }

            var pos = ComputeReplacementStartPosition(openRequest.Request.Code, openRequest.Request.CursorPosition);
            var reply = new CompleteReply(pos, openRequest.Request.CursorPosition, matches: completionRequestCompleted.CompletionList.Select(e => e.InsertText).ToList());

            var completeReply = Message.CreateResponse(reply, openRequest.Context.Request);
            openRequest.Context.ServerChannel.Send(completeReply);
            openRequest.Context.RequestHandlerStatus.SetAsIdle();
            openRequest.Dispose();
            
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