// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Extensions;
using Microsoft.DotNet.Interactive.Rendering;
using static Microsoft.DotNet.Interactive.Rendering.PocketViewTags;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public static class KernelExtensions
    {
        public static T UseDefaultMagicCommands<T>(this T kernel)
            where T : KernelBase
        {
            kernel.UseLsMagic()
                  .UseHtml()
                  .UseJavaScript();

            return kernel;
        }

        private static T UseHtml<T>(this T kernel)
            where T : KernelBase
        {
            kernel.AddDirective(new Command("%%html")
            {
                Handler =  CommandHandler.Create((KernelInvocationContext context) =>
                {
                    if (context.Command is SubmitCode submitCode)
                    {
                        var htmlContent = submitCode.Code
                                                      .Replace("%%html", "")
                                                      .Trim();

                      
                        context.Publish(new Events.DisplayedValueProduced(
                                           htmlContent,
                                           context.Command,
                                           formattedValues: new[]
                                           {
                                               new FormattedValue("text/html", htmlContent)
                                           }));
                        
                        context.Complete();
                    }
                })
            });

            return kernel;
        }

        private static T UseJavaScript<T>(this T kernel)
            where T : KernelBase
        {
            kernel.AddDirective(javascript());

            return kernel;
        }

        private static T UseLsMagic<T>(this T kernel)
            where T : KernelBase
        {
            kernel.AddDirective(lsmagic());

            kernel.VisitSubkernels(k =>
            {
                if (k is KernelBase kb)
                {
                    kb.AddDirective(lsmagic());
                }
            });

            Formatter<SupportedDirectives>.Register((directives, writer) =>
            {
                PocketView t = div(
                    h6(directives.KernelName),
                    p(directives.Commands.Select(Describe)));

                t.WriteTo(writer, HtmlEncoder.Default);
            }, "text/html");

            return kernel;

            IHtmlContent Describe(ICommand command)
            {
                return span(command.Name, " ");
            }
        }

        private static Command lsmagic()
        {
            return new Command("%lsmagic")
            {
                Handler = CommandHandler.Create(async (KernelInvocationContext context) =>
                {
                    var supportedDirectives = new SupportedDirectives(context.CurrentKernel.Name);

                    supportedDirectives.Commands.AddRange(context.CurrentKernel.Directives);

                    context.Publish(new Events.DisplayedValueProduced(supportedDirectives));

                    await context.CurrentKernel.VisitSubkernelsAsync(async k =>
                    {
                        if (k.Directives.Any(d => d.Name == "%lsmagic"))
                        {
                            await k.SendAsync(context.Command);
                        }
                    });
                })
            };
        }

        private static Command javascript()
        {
            return new Command("%%javascript")
            {
                Handler = CommandHandler.Create((KernelInvocationContext context) =>
                {
                    if (context.Command is SubmitCode submitCode)
                    {
                        var scriptContent = submitCode.Code
                                                      .Replace("%%javascript", "")
                                                      .Trim();

                        string value =
                            script[type: "text/javascript"](
                                HTML(
                                    scriptContent))
                                .ToString();

                        context.Publish(new Events.DisplayedValueProduced(
                                           scriptContent,
                                           context.Command,
                                           formattedValues: new[]
                                           {
                                               new FormattedValue("text/html",
                                                                  value)
                                           }));
                        context.Complete();
                    }
                })
            };
        }
    }

    public class SupportedDirectives
    {
        public string KernelName { get; }

        public SupportedDirectives(string kernelName)
        {
            KernelName = kernelName;
        }

        public List<ICommand> Commands { get; } = new List<ICommand>();
    }
}