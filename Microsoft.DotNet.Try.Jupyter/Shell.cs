// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.DotNet.Try.Jupyter.Protocol;
using Microsoft.Extensions.Hosting;
using NetMQ.Sockets;
using Pocket;
using Recipes;

namespace Microsoft.DotNet.Try.Jupyter
{
    public class Shell : IHostedService
    {
        private readonly ICommandScheduler<JupyterRequestContext> _scheduler;
        private readonly RouterSocket _shell;
        private readonly PublisherSocket _ioPubSocket;
        private readonly string _shellAddress;
        private readonly string _ioPubAddress;
        private readonly SignatureValidator _signatureValidator;
        private readonly CompositeDisposable _disposables;
        private readonly MessageSender _shellSender;
        private readonly MessageSender _ioPubSender;
        private readonly string _stdInAddress;
        private readonly string _controlAddress;
        private readonly RouterSocket _stdIn;
        private readonly RouterSocket _control;

        public Shell(
            ICommandScheduler<JupyterRequestContext> scheduler,
            ConnectionInformation connectionInformation)
        {
            if (connectionInformation == null)
            {
                throw new ArgumentNullException(nameof(connectionInformation));
            }

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

            _shellSender = new MessageSender(_shell, _signatureValidator);
            _ioPubSender = new MessageSender(_ioPubSocket, _signatureValidator);

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

            using (var activity = Logger<Shell>.Log.OnEnterAndExit())
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var message = _shell.GetMessage();

                    activity.Info("Received: {message}", message.ToJson());

                    var status = new RequestHandlerStatus(message.Header, new MessageSender(_ioPubSocket, _signatureValidator));
                    status.SetAsBusy();

                    switch (message.Header.MessageType)
                    {
                        case MessageTypeValues.KernelInfoRequest:
                            HandleKernelInfoRequest(message);
                            break;

                        case MessageTypeValues.KernelShutdownRequest:
                            break;

                        default:
                            var context = new JupyterRequestContext(
                                _shellSender,
                                _ioPubSender,
                                message,
                                new RequestHandlerStatus(message.Header, _shellSender));

                            await _scheduler.Schedule(context);

                            break;
                    }

                    status.SetAsIdle();
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _disposables.Dispose();
            return Task.CompletedTask;
        }

        private void HandleKernelInfoRequest(Message request)
        {
            var kernelInfoReply = new KernelInfoReply("5.1.0", ".NET", "5.1.0", new CSharpLanguageInfo());

            var replyMessage = Message.CreateResponse(kernelInfoReply, request);


            _shellSender.Send(replyMessage);
        }
    }
}