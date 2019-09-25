// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.Extensions.Hosting;
using NetMQ.Sockets;
using Pocket;
using Recipes;
using static Pocket.Logger<Microsoft.DotNet.Interactive.Jupyter.Shell>;
using InvalidOperationException = System.InvalidOperationException;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class Shell : IHostedService
    {
        private readonly IKernel _kernel;
        private readonly ICommandScheduler<JupyterRequestContext> _scheduler;
        private readonly RouterSocket _shell;
        private readonly PublisherSocket _ioPubSocket;
        private readonly string _shellAddress;
        private readonly string _ioPubAddress;
        private readonly SignatureValidator _signatureValidator;
        private readonly CompositeDisposable _disposables;
        private readonly IReplyChannel _shellSender;
        private readonly IPubSubChannel _ioPubSender;
        private readonly string _stdInAddress;
        private readonly string _controlAddress;
        private readonly RouterSocket _stdIn;
        private readonly RouterSocket _control;

        public Shell(
            IKernel kernel,
            ICommandScheduler<JupyterRequestContext> scheduler,
            ConnectionInformation connectionInformation)
        {
            if (connectionInformation == null)
            {
                throw new ArgumentNullException(nameof(connectionInformation));
            }

            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));

            _shellAddress = $"{connectionInformation.Transport}://{connectionInformation.IP}:{connectionInformation.ShellPort}";
            _ioPubAddress = $"{connectionInformation.Transport}://{connectionInformation.IP}:{connectionInformation.IOPubPort}";
            _stdInAddress = $"{connectionInformation.Transport}://{connectionInformation.IP}:{connectionInformation.StdinPort}";
            _controlAddress = $"{connectionInformation.Transport}://{connectionInformation.IP}:{connectionInformation.ControlPort}";

            var signatureAlgorithm = connectionInformation.SignatureScheme.Replace("-", string.Empty).ToUpperInvariant();
            _signatureValidator = new SignatureValidator(connectionInformation.Key, signatureAlgorithm);
            _shell = new RouterSocket();
            _ioPubSocket = new PublisherSocket();
            _stdIn = new RouterSocket();
            _control = new RouterSocket();

            _shellSender = new ReplyChannel( new MessageSender(_shell, _signatureValidator));
            _ioPubSender = new PubSubChannel( new MessageSender(_ioPubSocket, _signatureValidator));

            _disposables = new CompositeDisposable
                           {
                               _shell,
                               _ioPubSocket,
                               _stdIn,
                               _control
                           };
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _shell.Bind(_shellAddress);
            _ioPubSocket.Bind(_ioPubAddress);
            _stdIn.Bind(_stdInAddress);
            _control.Bind(_controlAddress);
            var id = Guid.NewGuid().ToString();
          
            using (var activity = Log.OnEnterAndExit())
            {
                //SetStarting();
                while (!cancellationToken.IsCancellationRequested)
                {
                    var request = _shell.GetMessage();

                    activity.Info("Received: {message}", request.ToJson());

                    SetBusy(request);

                    switch (request.Header.MessageType)
                    {
                        case JupyterMessageContentTypes.KernelInfoRequest:
                            id = Encoding.Unicode.GetString(request.Identifiers[0].ToArray());
                            HandleKernelInfoRequest(request);
                            SetIdle(request);
                            break;

                        case JupyterMessageContentTypes.KernelShutdownRequest:
                            SetIdle(request);
                            break;

                        default:
                            var context = new JupyterRequestContext(
                                _shellSender,
                                _ioPubSender,
                                request, id);

                            await _scheduler.Schedule(context);

                            await context.Done();

                            SetIdle(request);

                            break;
                    }

                    
                }

                void SetBusy(Message request) => _ioPubSender.Publish(new Status(StatusValues.Busy), request, id);

                void SetIdle(Message request) => _ioPubSender.Publish(new Status(StatusValues.Idle), request, id);

                
            }

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _disposables.Dispose();
            return Task.CompletedTask;
        }

        private void HandleKernelInfoRequest(Message request)
        {
            var languageInfo = GetLanguageInfo();
            var kernelInfoReply = new KernelInfoReply(Constants.MESSAGE_PROTOCOL_VERSION, ".NET", "5.1.0", languageInfo);
            _shellSender.Reply(kernelInfoReply, request);
        }

        private LanguageInfo GetLanguageInfo()
        {
            switch (_kernel)
            {
                case CompositeKernel composite:
                    return GetLanguageInfo(composite.DefaultKernelName);
                case IKernel kernel:
                    return GetLanguageInfo(kernel.Name);
                default:
                    throw new InvalidOperationException();
            }
        }

        private LanguageInfo GetLanguageInfo(string kernelName)
        {
            switch (kernelName)
            {
                case "csharp":
                    return new CSharpLanguageInfo();
                case "fsharp":
                    return new FSharpLanguageInfo();
                default:
                    throw new InvalidOperationException($"{kernelName} not supported");
            }
        }
    }
}