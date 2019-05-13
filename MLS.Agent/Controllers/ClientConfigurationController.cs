// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Try.Protocol;
using Pocket;
using Recipes;
using static Pocket.Logger<MLS.Agent.Controllers.ClientConfigurationController>;
using HttpRequest = Microsoft.AspNetCore.Http.HttpRequest;

namespace MLS.Agent.Controllers
{
    public class ClientConfigurationController : Controller
    {
        private const string ClientConfiguration = "/clientConfiguration";
        public static RequestDescriptor ClientConfigurationApi => new RequestDescriptor(ClientConfiguration);

        [HttpPost]
        [Route(ClientConfiguration)]
        public async Task<IActionResult> ConfigurationAsync()
        {
            using (var operation = Log.ConfirmOnExit())
            {
                var requestBody = await ReadBody(Request);

                var links = new RequestDescriptors(new RequestDescriptor(Request.Path, Request.Method, requestBody: requestBody))
                {
                    Configuration = ClientConfigurationController.ClientConfigurationApi,
                    Completion = LanguageServicesController.CompletionApi,
                    AcceptCompletion = new RequestDescriptor("{acceptanceUri}", templated: true),
                    LoadFromGist = new RequestDescriptor("/workspace/fromgist/{gistId}/{commitHash?}", method: "GET",templated: true,
                        properties: new[]
                        {
                            new RequestDescriptorProperty("workspaceType"),
                            new RequestDescriptorProperty("extractBuffers")
                        }),
                    Diagnostics = LanguageServicesController.DiagnosticsApi,
                    SignatureHelp = LanguageServicesController.SignatureHelpApi,
                    Snippet = new RequestDescriptor("/snippet",method: "GET",
                        properties: new[]
                        {
                            new RequestDescriptorProperty("from"),
                        }),
                    Run = RunController.RunApi,
                    Compile = CompileController.CompileApi,
                    Version = SensorsController.VersionApi,
                    ProjectFromGist = new RequestDescriptor("/project/fromGist"),
                    RegionsFromFiles = ProjectController.RegionsFromFilesApi,
                    GetPackage = PackagesController.GetPackageApi
                };

                var versionId = links.ComputeHash();
                var clientConfig = new ClientConfiguration(versionId, links, 30000, string.Empty, false);
                operation.Succeed();
                return Ok(clientConfig);
            }
        }

        private static async Task<string> ReadBody(HttpRequest request)
        {
            string body = null;
            if (request.Body != null)
            {
                using (var reader = new StreamReader(request.Body))
                {
                    body = await reader.ReadToEndAsync();
                }
            }
            return body;
        }
    }
}