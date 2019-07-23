using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using WorkspaceServer.PackageRestore;
using Xunit;

namespace WorkspaceServer.Tests
{
    public class PackageRestoreContextTests
    {
        [Fact]
        public async Task Returns_new_references_if_they_are_added()
        {
            var refs = await new PackageRestoreContext().AddPackage("FluentAssertions", "5.7.0");
            refs.Should().Contain(r => r.Display.Contains("FluentAssertions.dll"));
            refs.Should().Contain(r => r.Display.Contains("System.Configuration.ConfigurationManager"));
        }

        [Fact]
        public async Task Returns_null_if_package_installation_fails()
        {
            var refs = await new PackageRestoreContext().AddPackage("not-a-real-package-definitely-not", "5.7.0");
            refs.Should().BeEmpty();
        }
    }
}
