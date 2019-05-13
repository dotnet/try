// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;

namespace MLS.Agent
{
    public class StaticFilesProxyRouter : IRouter
    {
        // TODO: (StaticFilesProxyRouter) remove this class and move these resources into the agent repo so they can be served locally without need for an internet connection
        private readonly HttpClient _httpClient = new HttpClient
                                                  {
                                                      BaseAddress = new Uri("http://localhost:27261/")
                                                  };

        public async Task RouteAsync(RouteContext context)
        {

            var path = context.HttpContext.Request.Path;

            if (path.Value.EndsWith(".js") ||
                path.Value.EndsWith(".css") ||
                path.Value.EndsWith(".png") ||
                path.Value.EndsWith(".ico"))
            {
                var response = await _httpClient.GetAsync(path.Value);

                if (response.IsSuccessStatusCode)
                {
                    context.Handler = async httpContext =>
                    {
                        var responseFromTryDotNet = await response.Content.ReadAsStreamAsync();

                        await responseFromTryDotNet.CopyToAsync(httpContext.Response.Body);
                    };
                }
            }
        }

        public VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            return null;
        }
    }
}