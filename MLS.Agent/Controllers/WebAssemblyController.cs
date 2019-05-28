using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Try.Markdown;
using MLS.Agent.CommandLine;
using MLS.Agent.Markdown;
using Recipes;
using WorkspaceServer;
using WorkspaceServer.Packaging;

namespace MLS.Agent.Controllers
{
    public class WebAssemblyController : Controller
    {
        private StartupOptions _startupOptions;
        private PackageRegistry _registry;

        public WebAssemblyController(StartupOptions startupOptions, PackageRegistry packageRegistry)
        {
            _registry = packageRegistry ?? throw new ArgumentNullException(nameof(packageRegistry));
        }

        [HttpGet]
        [Route("/LocalCodeRunner/{package}")]
        [Route("/LocalCodeRunner/{package}/")]
        [Route("/LocalCodeRunner/{package}/{*requestedPath}")]
        public async Task<IActionResult> GetFile(string package, string requestedPath)
        {
            var end = requestedPath.IndexOf("/");
            if (end == -1)
            {
                end = requestedPath.Length;
            }

            var firstSegment = requestedPath.Substring(0, end);
            var package = await _registry.Get<Package2>(firstSegment);
            var asset = package.Assets.OfType<WebAssemblyAsset>().FirstOrDefault();
            if (asset == null)
            {
                return NotFound();
            }

            var file = asset.DirectoryAccessor.GetFullyQualifiedPath(new RelativeFilePath(requestedPath));
            if (!file.Exists)
            {
                file = asset.DirectoryAccessor.GetFullyQualifiedFilePath("index.html");
                return await FileContents(file);
            }

            return await FileContents(file);
        }

        private async Task<IActionResult> FileContents(FileSystemInfo file)
        {
            var contentType = GetContentType(file.FullName);
            var bytes = await System.IO.File.ReadAllBytesAsync(file.FullName);

            return File(bytes, contentType);
        }

        private string GetContentType(string path)
        {
            var extension = Path.GetExtension(path);
            switch (extension)
            {
                case ".dll":
                    return "application/octet-stream";
                case ".json":
                    return "application/json";
                case ".wasm":
                    return "application/wasm";
                case ".woff":
                    return "application/font-woff";
                case ".woff2":
                    return "application/font-woff";
                case ".js":
                    return "application/javascript";
                default:
                    return "text/html";
            }
        }
    }
}