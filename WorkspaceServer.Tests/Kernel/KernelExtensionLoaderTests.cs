using System.Threading.Tasks;
using Xunit;
using WorkspaceServer.Tests.Kernel;
using Xunit.Abstractions;
using Microsoft.DotNet.Interactive;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Events;

namespace WorkspaceServer.Tests
{
    public class KernelExtensionLoaderTests : CSharpKernelTestBase
    {
        public KernelExtensionLoaderTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Can_load_from_assembly()
        {
            var directory = Create.EmptyWorkspace().Directory;
            var extensionDll = await KernelExtensionTestHelper.CreateExtension(directory);

            var kernel = CreateKernel();
            await new KernelExtensionLoader().TryLoadFromAssembly(extensionDll, kernel);

            KernelEvents.Should()
                      .ContainSingle(e => e.Value is CodeSubmissionEvaluated &&
                                          e.Value.As<CodeSubmissionEvaluated>().Code.Contains("using System.Reflection;"));

        }
    }
}