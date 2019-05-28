using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Try.Markdown;
using MLS.Agent.CommandLine;
using WorkspaceServer;
using WorkspaceServer.Packaging;

namespace MLS.Agent.Controllers
{
    public class WebAssemblyController : Controller
    {
        private PackageRegistry _registry;

        public WebAssemblyController(PackageRegistry packageRegistry)
        {
            _registry = packageRegistry ?? throw new ArgumentNullException(nameof(packageRegistry));
        }

        [HttpGet]
        [Route("/LocalCodeRunner/{packageName}/")]
        [Route("/LocalCodeRunner/{packageName}/{*requestedPath}")]
        public async Task<IActionResult> GetFile(string packageName, string requestedPath = "Index.html")
        {
            var package = await _registry.Get<Package2>(packageName);
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