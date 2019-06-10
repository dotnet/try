// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Try.Project;
using Microsoft.DotNet.Try.Protocol;
using Microsoft.DotNet.Try.Protocol.ClientApi;
using Pocket;
using static Pocket.Logger<MLS.Agent.Controllers.ProjectController>;
using SourceFile = Microsoft.DotNet.Try.Protocol.ClientApi.SourceFile;

namespace MLS.Agent.Controllers
{
    public class ProjectController : Controller
    {

        private const string RegionsFromFilesRoute = "/project/files/regions";
        public static RequestDescriptor RegionsFromFilesApi => new RequestDescriptor(RegionsFromFilesRoute, method: "POST");

        [HttpPost(RegionsFromFilesRoute)]
        public IActionResult GenerateRegionsFromFiles([FromBody] CreateRegionsFromFilesRequest request)
        {
            using (var operation = Log.OnEnterAndConfirmOnExit())
            {
                var regions = request.Files.SelectMany(ExtractRegions);
                var response = new CreateRegionsFromFilesResponse(request.RequestId, regions.ToArray());

                IActionResult result = Ok(response);
                operation.Succeed();

                return result;
            }
        }

        private static IEnumerable<SourceFileRegion> ExtractRegions(SourceFile sourceFile)
        {
            var sc = SourceText.From(sourceFile.Content);
            var regions = sc.ExtractRegions(sourceFile.Name).Select(
                region => new SourceFileRegion(region.bufferId.ToString(), sc.ToString(region.span).FormatSourceCode(sourceFile.Name))).ToArray();
            return regions;
        }

    }
}
