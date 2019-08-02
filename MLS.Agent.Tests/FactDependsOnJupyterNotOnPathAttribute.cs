// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using WorkspaceServer;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace MLS.Agent.Tests
{
    [XunitTestCaseDiscoverer("MLS.Agent.Tests.JupyterNotInstalledTestCaseDiscover", "MLS.Agent.Tests")]
    public class FactDependsOnJupyterNotOnPathAttribute : FactAttribute
    {
    }

    public class JupyterNotInstalledTestCaseDiscover : IXunitTestCaseDiscoverer
    {
        private readonly IMessageSink messageSink;

        public JupyterNotInstalledTestCaseDiscover(IMessageSink messageSink)
        {
            this.messageSink = messageSink;
        }

        public IEnumerable<IXunitTestCase> Discover(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            ITestMethod testMethod,
            IAttributeInfo factAttribute)
        {
            if (testMethod.TestClass.Class.Name.Contains("Integration") && FileSystemJupyterKernelSpec.CheckIfJupyterKernelSpecExists())
            {
                yield break;
            }

            yield return new XunitTestCase(
                messageSink,
                TestMethodDisplay.ClassAndMethod,
                new TestMethodDisplayOptions(),
                testMethod
            );
        }
    }
}