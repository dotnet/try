// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MLS.Agent.Tools;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class JupyterPathInfo
    {
        public static JupyterDataPathResult GetDataPaths(CommandLineResult jupyterPathResult)
        {
            if (jupyterPathResult.ExitCode == 0)
            {
                if (TryGetDataPaths(jupyterPathResult.Output.ToArray(), out var dataPaths))
                {
                    return new JupyterDataPathResult(dataPaths);
                }
                else
                {
                    return new JupyterDataPathResult($"Could not find the jupyter kernel installation directory." +
                            $" Output of \"jupyter --paths\" is {string.Join("\n", jupyterPathResult.Output.ToArray())}");
                }
            }
            else
            {
                return new JupyterDataPathResult($"Tried to invoke \"jupyter --paths\" but failed with exception: {string.Join("\n", jupyterPathResult.Error)}");
            }
        }

        private static bool TryGetDataPaths(string[] pathInfo, out IEnumerable<DirectoryInfo> dataPaths)
        {
            var dataHeaderIndex = Array.FindIndex(pathInfo, element => element.Trim().CompareTo("data:") == 0);
            if (dataHeaderIndex != -1)
            {
                var nextHeaderIndex = Array.FindIndex(pathInfo, dataHeaderIndex + 1, element => element.Trim().EndsWith(":"));
                if (nextHeaderIndex == -1)
                    nextHeaderIndex = pathInfo.Count();

                dataPaths = pathInfo.Skip(dataHeaderIndex + 1).Take(nextHeaderIndex - dataHeaderIndex - 1).Select(dir => new DirectoryInfo(dir.Trim()));
                return true;
            }

            dataPaths = Enumerable.Empty<DirectoryInfo>();
            return false;
        }
    }
}