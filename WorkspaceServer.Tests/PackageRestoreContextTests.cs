using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using WorkspaceServer.PackageRestore;
using Xunit;

namespace WorkspaceServer.Tests
{
    public class PackageRestoreContextTests
    {
        [Fact]
        public async Task It_works()
        {
            using (var context = new PackageRestoreContext())
            {
                var refs = await context.AddPackage("FluentAssertions", "5.7.0");
                refs.ToArray().Length.Should().Be(2);
            }
        }
    }
}
