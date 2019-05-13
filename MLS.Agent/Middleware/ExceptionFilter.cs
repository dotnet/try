// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Pocket;
using static Pocket.Logger<MLS.Agent.Middleware.ExceptionFilter>;

namespace MLS.Agent.Middleware
{
    public class ExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            var exception = context.Exception;

            if (exception == null)
            {
                return;
            }

            if (context.ModelState.ErrorCount > 0)
            {
                context.Result = new BadRequestResult();
            }
            else
            {
                context.Result = new ExceptionResult(exception);

                if (context.ExceptionHandled)
                {
                    Log.Warning(exception);
                }
                else
                {
                    Log.Error(exception);
                }
            }
        }

        private class ExceptionResult : IActionResult
        {
            private readonly Exception exception;

            public ExceptionResult(Exception exception)
            {
                this.exception = exception;
            }

            public async Task ExecuteResultAsync(ActionContext context)
            {
                var objectResult = new ObjectResult(new
                {
                    message = "An unhandled exception occurred.",
                    exception = exception.ToString()
                })
                {
                    StatusCode = exception.ToHttpStatusCode()
                };

                await objectResult.ExecuteResultAsync(context);
            }
        }
    }
}
