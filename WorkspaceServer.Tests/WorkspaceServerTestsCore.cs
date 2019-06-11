// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Pocket;
using Xunit.Abstractions;

namespace WorkspaceServer.Tests
{
    public abstract class WorkspaceServerTestsCore : IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        protected WorkspaceServerTestsCore(ITestOutputHelper output)
        {
            _disposables.Add(output.SubscribeToPocketLogger());
        }

        public void Dispose() => _disposables.Dispose();

        protected abstract Task<ILanguageService> GetLanguageServiceAsync();

        protected abstract Task<ICodeCompiler> GetCodeCompilerAsync();

        protected abstract Task<ICodeRunner> GetCodeRunnerAsync();
    }
}