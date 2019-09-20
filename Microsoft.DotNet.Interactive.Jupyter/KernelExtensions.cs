// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using Markdig;
using Markdig.Renderers;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Extensions;
using Microsoft.DotNet.Interactive.Rendering;
using MLS.Agent.Tools;
using MLS.Agent.Tools.Roslyn;
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
                  .UseJavaScript()
                  .UseMarkdown()
                  .UseTime()
                  .UseWriteFile();

            return kernel;
        }

        private static T UseHtml<T>(this T kernel)
            where T : KernelBase
        {
            kernel.AddDirective(new Command("%%html")
            {
                Handler = CommandHandler.Create((KernelInvocationContext context) =>
               {
                   if (context.Command is SubmitCode submitCode)
                   {
                       var htmlContent = submitCode.Code
                                                     .Replace("%%html", "")
                                                     .Trim();

                       context.Publish(new DisplayedValueProduced(
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

        private static T UseMarkdown<T>(this T kernel)
            where T : KernelBase
        {
            var pipeline = new MarkdownPipelineBuilder()
                   .UseMathematics()
                   .UseAdvancedExtensions()
                   .Build();

            kernel.AddDirective(new Command("%%markdown")
            {
                Handler = CommandHandler.Create((KernelInvocationContext context) =>
                {
                    if (context.Command is SubmitCode submitCode)
                    {
                        var markdown = submitCode.Code
                                                 .Replace("%%markdown", "")
                                                 .Trim();

                        var document = Markdown.Parse(
                            markdown,
                            pipeline);

                        string html;

                        using (var writer = new StringWriter())
                        {
                            var renderer = new HtmlRenderer(writer);
                            pipeline.Setup(renderer);
                            renderer.Render(document);
                            html = writer.ToString();
                        }

                        context.Publish(
                            new DisplayedValueProduced(
                                html,
                                context.Command,
                                new[]
                                {
                                    new FormattedValue("text/html", html)
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

        private static T UseTime<T>(this T kernel)
            where T : KernelBase
        {
            kernel.AddDirective(time());

            return kernel;
        }

        private static T UseWriteFile<T>(this T kernel)
           where T : KernelBase
        {
            kernel.AddDirective(writefile());

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
                    pre(directives.Commands.Select(Describe)));

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

                    context.Publish(new DisplayedValueProduced(supportedDirectives, context.Command));

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

                        context.Publish(new DisplayedValueProduced(
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

        private static Command time()
        {
            return new Command("%%time")
            {
                Handler = CommandHandler.Create(async (KernelInvocationContext context) =>
                {
                    if (context.Command is SubmitCode submitCode)
                    {
                        var code = submitCode.Code
                                             .Replace("%%time", "")
                                             .Trim();

                        var timer = new Stopwatch();
                        timer.Start();

                        await context.CurrentKernel.SendAsync(
                            new SubmitCode(code, submitCode.TargetKernelName));

                        var elapsed = timer.Elapsed;

                        var formattableString = $"Wall time: {elapsed.TotalMilliseconds}ms";

                        context.Publish(
                            new DisplayedValueProduced(
                                elapsed,
                                context.Command,
                                new[]
                                {
                                    new FormattedValue(PlainTextFormatter.MimeType, formattableString)
                                }));

                        context.Complete();
                    }
                })
            };
        }

        private static Command writefile()
        {
            var command = new Command("%%writefile")
            {
                Handler = CommandHandler.Create(async (KernelInvocationContext context, WriteFileOptions options) =>
                {
                    if (context.Command is SubmitCode submitCode)
                    {
                        var code = string.Join("\n", submitCode.Code.Split('\n', '\r').Where(line => !line.Contains("%%writefile")));
                                            
                        try
                        {
                            if (!options.FileName.Exists)
                            {
                                options.FileName.Create().Dispose();
                            }

                            string message;
                            if (options.Append)
                            {
                                File.AppendAllText(options.FileName.FullName, code);
                                message = $"Appending text to file {options.FileName.FullName}";
                            }
                            else
                            {
                                File.WriteAllText(options.FileName.FullName, code);
                                message = $"Overwriting text to file {options.FileName.FullName}";
                            }
                            await context.HandlingKernel.SendAsync(new DisplayValue(message, new FormattedValue(PlainTextFormatter.MimeType, message)));
                        }
                        catch (Exception e)
                        {
                            var formattableString = $"Could not write to file {options.FileName.FullName} due to exception {e.Message}";
                            await context.HandlingKernel.SendAsync(new DisplayValue(formattableString, new FormattedValue(PlainTextFormatter.MimeType, formattableString)));
                        }

                        context.Complete();
                    }
                })
            };

            var fileArgument = new Argument<FileInfo>()
            {
                Name = nameof(WriteFileOptions.FileName),
                Description = "Specify the file path to write to"
            };

            fileArgument.AddValidator(symbolResult =>
            {
                var file = symbolResult.Tokens
                               .Select(t => t.Value)
                               .FirstOrDefault();
                
                if (!PathUtilities.IsAbsolute(file))
                {
                    return "Absolute file path expected";
                }

                if (!PathUtilities.IsValidFilePath(file))
                {
                    return "Invalid file path";
                }

                return null;
            });

            command.AddArgument(fileArgument);
            command.AddOption(new Option(new string[] { "-a", "--append" }, "Append to file")
            {
                Argument = new Argument<bool>(defaultValue: () => false)
            });

            return command;
        }
    }
}