using System;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using WorkspaceServer.Packaging;
using Xunit;

namespace WorkspaceServer.Tests
{
    public class PackageRestoreContextTests
    {
        [Fact]
        public async Task Returns_new_references_if_they_are_added()
        {
            var result = await new PackageRestoreContext().AddPackage("FluentAssertions", "5.7.0");
            result.Succeeded.Should().BeTrue();
            var refs = result.References;
            refs.Should().Contain(r => r.Display.Contains("FluentAssertions.dll"));
            refs.Should().Contain(r => r.Display.Contains("System.Configuration.ConfigurationManager"));
        }

        [Fact]
        public async Task Returns_failure_if_package_installation_fails()
        {
            var result = await new PackageRestoreContext().AddPackage("not-a-real-package-definitely-not", "5.7.0");
            result.Succeeded.Should().BeFalse();
        }
    }
}