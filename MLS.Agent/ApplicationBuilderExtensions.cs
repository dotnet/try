// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
using Recipes;

namespace MLS.Agent
{
    internal static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder EnableCachingBlazorContent(this IApplicationBuilder app)
        {
            return app.Use((context, next) =>
            {
                if (HttpMethods.IsGet(context.Request.Method))
                {                    
                    context.Response.Headers[HeaderNames.CacheControl] = "public, max-age=604800";
                }

                return next();
            });
        }

        public static IApplicationBuilder UseStaticFilesFromToolLocationAndRootDirectory(this IApplicationBuilder app, DirectoryInfo rootDirectory)
        {
            var options = GetStaticFilesOptions(rootDirectory);

            app.UseStaticFiles();
            if (options != null)
            {
                app.UseStaticFiles(options);
            }
            

            return app;
        }

        private static StaticFileOptions GetStaticFilesOptions(DirectoryInfo rootDirectory)
        {
            var paths = new List<string>
            {
                Path.Combine(Path.GetDirectoryName(typeof(Startup).Assembly.Location), "wwwroot"),
                rootDirectory.FullName
            };

            var providers = paths.Where(Directory.Exists).Select(p => new PhysicalFileProvider(p)).ToArray();

            StaticFileOptions options = null;

            if (providers.Length > 0)
            {
                var combinedProvider = new CompositeFileProvider(providers);

                var sharedOptions = new SharedOptions { FileProvider = combinedProvider };
                options = new StaticFileOptions(sharedOptions);
            }

            return options;
        }
    }
}