// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Try.Markdown;
using MLS.Agent.CommandLine;
using MLS.Agent.Markdown;
using Recipes;
using WorkspaceServer;
using WorkspaceServer.Packaging;

namespace MLS.Agent.Controllers
{
    public class DocumentationController : Controller
    {
        private readonly MarkdownProject _markdownProject;
        private readonly StartupOptions _startupOptions;
        private readonly PackageRegistry _packageRegistry;
        private static readonly string _cacheBuster = VersionSensor.Version().AssemblyVersion;

        public DocumentationController(MarkdownProject markdownProject, StartupOptions startupOptions, PackageRegistry packageRegistry)
        {
            _markdownProject = markdownProject ??
                               throw new ArgumentNullException(nameof(markdownProject));
            _startupOptions = startupOptions;
            _packageRegistry = packageRegistry ??
                               throw new ArgumentNullException(nameof(packageRegistry));
        }

        [HttpGet]
        [Route("{*path:regex(.*.md?$)}")]
        public async Task<IActionResult> ShowMarkdownFile(string path)
        {
            if (_startupOptions.Mode != StartupMode.Try)
            {
                return NotFound();
            }

            var relativeFilePath = new RelativeFilePath(path);

            if (!_markdownProject.TryGetMarkdownFile(relativeFilePath, out var markdownFile))
            {
                return NotFound();
            }

            var hostUrl = Request.GetUri();

            var blocks = (await markdownFile.GetEditableAnnotatedCodeBlocks()).ToArray();

            var maxEditorPerSession = blocks.Length > 0
                                          ? blocks
                                              .GroupBy(b => b.Annotations.Session)
                                              .Max(editors => editors.Count())
                                          : 0;

            var pipeline = _markdownProject.GetMarkdownPipelineFor(markdownFile.Path);

            var extension = pipeline.Extensions.FindExact<CodeBlockAnnotationExtension>();

            if (extension != null)
            {
                extension.InlineControls = maxEditorPerSession <= 1;
                extension.EnablePreviewFeatures = _startupOptions.EnablePreviewFeatures;

            }



            var content = maxEditorPerSession <= 1
                              ? await OneColumnLayoutScaffold(
                                    $"{hostUrl.Scheme}://{hostUrl.Authority}",
                                    markdownFile)
                              : await TwoColumnLayoutScaffold(
                                    $"{hostUrl.Scheme}://{hostUrl.Authority}",
                                    markdownFile);

            return Content(content.ToString(), "text/html");
        }

        [HttpGet]
        [Route("/")]
        public async Task<IActionResult> ShowIndex()
        {
            const string documentSvg = "<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" version=\"1.1\" width=\"24\" height=\"24\" viewBox=\"0 0 24 24\"><path d=\"M6,2A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2H6M6,4H13V9H18V20H6V4M8,12V14H16V12H8M8,16V18H13V16H8Z\" /></svg>";
            var links = string.Join(
                "\n",
                _markdownProject.GetAllMarkdownFiles()
                                .Select(f =>
                                 $@"<li><a href=""{f.Path.Value.HtmlAttributeEncode()}"">{documentSvg}<span>{f.Path.Value}</span></a></li>"));

            return Content(Index(links).ToString(), "text/html");
        }


        public static async Task<IHtmlContent> SessionControlsHtml(MarkdownFile markdownFile, bool enablePreviewFeatures = false)
        {
            var sessions = (await markdownFile
                   .GetAnnotatedCodeBlocks())
                   .GroupBy(b => b.Annotations.Session);

            var sb = new StringBuilder();

            foreach (var session in sessions)
            {
                sb.AppendLine($@"<button class=""run"" data-trydotnet-mode=""run"" data-trydotnet-session-id=""{session.Key}"" data-trydotnet-run-args=""{session.First().Annotations.RunArgs.HtmlAttributeEncode()}"">{session.Key}{SvgResources.RunButtonSvg}</button>");

                sb.AppendLine(enablePreviewFeatures
                    ? $@"<div class=""output-panel"" data-trydotnet-mode=""runResult"" data-trydotnet-output-type=""terminal"" data-trydotnet-session-id=""{session.Key}""></div>"
                    : $@"<div class=""output-panel"" data-trydotnet-mode=""runResult"" data-trydotnet-session-id=""{session.Key}""></div>");
            }

            return new HtmlString(sb.ToString());
        }

        private async Task<AutoEnableOptions> GetAutoEnableOptions(MarkdownFile file)
        {
            bool useWasmRunner;

            if (_startupOptions.Package != null)
            {
                var package = await _packageRegistry.Get<Package2>(_startupOptions.Package);
                useWasmRunner = package.CanSupportWasm;
            }
            else
            {
                var blocks = await file.GetAnnotatedCodeBlocks();
                var packageUsesWasm = await Task.WhenAll(blocks
                    .Select(b => b.PackageName())
                    .Select(async name => (await _packageRegistry.Get<ICanSupportWasm>(name))?.CanSupportWasm ?? false));

                useWasmRunner = packageUsesWasm.Any(p => p);
            }

            var requestUri = Request.GetUri();

            var hostUrl = $"{requestUri.Scheme}://{requestUri.Authority}";
            return new AutoEnableOptions(hostUrl, useWasmRunner);
        }

        private class AutoEnableOptions
        {
            public AutoEnableOptions(string apiBaseAddress, bool useWasmRunner)
            {
                ApiBaseAddress = apiBaseAddress;
                UseWasmRunner = useWasmRunner;
            }

            public string ApiBaseAddress { get; }

            public bool UseWasmRunner { get; }
        }

        private IHtmlContent Layout(
            string hostUrl,
            MarkdownFile markdownFile,
            IHtmlContent content,
            AutoEnableOptions autoEnableOptions) =>
            $@"
<!DOCTYPE html>
<html lang=""en"">

<head>
    <meta http-equiv=""Content-Type"" content=""text/html;charset=utf-8"">
    <script src=""/api/trydotnet.min.js?v={_cacheBuster}""></script>
    <link rel=""stylesheet"" href=""/css/trydotnet.css?v={_cacheBuster}"">  
    <link rel=""icon"" type=""image/png"" href=""favicon-32x32.png"">
    {MathSupport()}
    <title>dotnet try - {markdownFile.Path.Value.HtmlEncode()}</title>
</head>

<body>
    {Header()}
    <section>
       {content}
    </section>

    {Footer()}

    <script>
        trydotnet.autoEnable({{ apiBaseAddress: new URL(""{autoEnableOptions.ApiBaseAddress}""), useWasmRunner: {autoEnableOptions.UseWasmRunner.ToString().ToLowerInvariant()} }});
    </script>
</body>

</html>".ToHtmlContent();

        private IHtmlContent MathSupport() =>
            @"
    <script type=""text/x-mathjax-config"">
        MathJax.Hub.Config({
        tex2jax: {inlineMath: [[""$"",""$""],[""\\("",""\\)""]]},       
        showMathMenu: false,
        showMathMenuMSIE: false
    });
    </script>
    <script type=""text/javascript"" src=""https://cdnjs.cloudflare.com/ajax/libs/mathjax/2.7.5/MathJax.js?config=TeX-AMS_SVG""></script>".ToHtmlContent();

        private async Task<IHtmlContent> OneColumnLayoutScaffold(string hostUrl, MarkdownFile markdownFile) =>
            Layout(
                hostUrl,
                markdownFile,
                await DocumentationDiv(markdownFile),
                await GetAutoEnableOptions(markdownFile));

        private static async Task<IHtmlContent> DocumentationDiv(MarkdownFile markdownFile) =>
            $@"<div id=""documentation-container"" class=""markdown-body"">
                {await markdownFile.ToHtmlContentAsync()}
            </div>".ToHtmlContent();

        private async Task<IHtmlContent> TwoColumnLayoutScaffold(string hostUrl, MarkdownFile markdownFile) =>
            Layout(hostUrl, markdownFile,
                   $@"{await DocumentationDiv(markdownFile)}
            <div class=""control-column"">
                {await SessionControlsHtml(markdownFile, _startupOptions.EnablePreviewFeatures)}
            </div>".ToHtmlContent(),
                   await GetAutoEnableOptions(markdownFile));

        private IHtmlContent Index(string html) =>
            $@"
<!DOCTYPE html>
<html lang=""en"">

<head>
    <meta http-equiv=""Content-Type"" content=""text/html;charset=utf-8"">
    <link rel=""stylesheet"" href=""/css/trydotnet.css?v={_cacheBuster}"">
    <title>dotnet try - {_startupOptions.Dir.FullName.HtmlEncode()}</title>
</head>

<body>
    {Header()}
    <section>
        <ul class=""index"">
            {html}
        </ul>
    </section>

    {Footer()}

</body>

</html>".ToHtmlContent();

        private IHtmlContent Header() => $@"
<header>
    <div>
        <span class=""dotnet-try"">Try .NET</span>
        <span class=""project-file-path"">{_startupOptions.Dir.FullName.ToLowerInvariant().HtmlEncode()}</span>
    </div>
    <a href=""https://dotnet.microsoft.com/platform/try-dotnet"">Powered by Try .NET</a>
</header>".ToHtmlContent();

        private IHtmlContent Footer() => @"
<footer>
    <ul>
        <li>
            <a href=""https://teams.microsoft.com/l/channel/19%3a32c2f8c34d4b4136b4adf554308363fc%40thread.skype/Try%2520.NET?groupId=fdff90ed-0b3b-4caa-a30a-efb4dd47665f&tenantId=72f988bf-86f1-41af-91ab-2d7cd011db47"">Ask a question or tell us about a bug</a>
        </li>
        <li>
            <a href=""https://dotnet.microsoft.com/platform/support-policy"">Support Policy</a>
        </li>
        <li>
            <a href=""https://go.microsoft.com/fwlink/?LinkId=521839"">Privacy &amp; Cookies</a>
        </li>
        <li>
            <a href=""https://go.microsoft.com/fwlink/?LinkID=206977"">Terms of Use</a>
        </li>
        <li>
            <a href=""https://www.microsoft.com/trademarks"">Trademarks</a>
        </li>
        <li>
            © Microsoft 2019
        </li>
    </ul>
</footer>".ToHtmlContent();
    }
}