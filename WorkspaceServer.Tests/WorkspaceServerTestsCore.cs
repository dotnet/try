// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Pocket;
using WorkspaceServer.Packaging;
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
        
        protected abstract Task<(ICodeRunner runner, Package workspace)> GetRunnerAndWorkspaceBuild(
            [CallerMemberName] string testName = null);

        protected abstract ILanguageService GetLanguageService(
            [CallerMemberName] string testName = null);
    }
}