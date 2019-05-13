// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Try.Protocol;
using Recipes;

namespace MLS.Agent.Controllers
{
    public class SensorsController : Controller
    {
        private const string VersionRoute = "/sensors/version";
        public static RequestDescriptor VersionApi => new RequestDescriptor(VersionRoute, method: "GET");

        [Route(VersionRoute)]
        public IActionResult GetVersion() => Ok(VersionSensor.Version());
    }
}