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
    [XunitTestCaseDiscoverer("MLS.Agent.Tests.TestCaseDiscoverer", "MLS.Agent.Tests")]
    public class FactSkippedForIntegrationAttribute : FactAttribute
    {
    }

    public class TestCaseDiscoverer : IXunitTestCaseDiscoverer
    {
        private readonly IMessageSink messageSink;

        public TestCaseDiscoverer(IMessageSink messageSink)
        {
            this.messageSink = messageSink;
        }

        public IEnumerable<IXunitTestCase> Discover(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            ITestMethod testMethod,
            IAttributeInfo factAttribute)
        {
            if (!File.Exists(Paths.JupyterKernelSpecPath))
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