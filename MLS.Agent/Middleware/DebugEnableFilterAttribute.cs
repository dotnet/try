// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Clockwise;
using Microsoft.AspNetCore.Mvc.Filters;
using Pocket;

namespace MLS.Agent.Middleware
{
    public class DebugEnableFilterAttribute : ActionFilterAttribute
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (Debugger.IsAttached && !(Clock.Current is VirtualClock))
            {
                _disposables.Add(VirtualClock.Start());
            }
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            _disposables.Dispose();
        }
    }
}

