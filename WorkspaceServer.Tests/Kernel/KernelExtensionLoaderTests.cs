using System.Threading.Tasks;
using Xunit;
using WorkspaceServer.Tests.Kernel;
using Xunit.Abstractions;
using Microsoft.DotNet.Interactive;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Events;
using System.Collections.Generic;

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
            var extensionDll = await KernelExtensionTestHelper.CreateExtension(directory, @"await kernel.SendAsync(new SubmitCode(""using System.Reflection;""));");

            var kernel = CreateKernel();
            var extensionLoadEvents = new List<IKernelEvent>();
            await new KernelExtensionLoader().LoadFromAssembly(extensionDll, kernel, (kernelEvent) => extensionLoadEvents.Add(kernelEvent));

            KernelEvents.Should()
                      .ContainSingle(e => e.Value is CodeSubmissionEvaluated &&
                                          e.Value.As<CodeSubmissionEvaluated>().Code.Contains("using System.Reflection;"));

        }

        [Fact]
        public async Task Gives_kernel_extension_load_exception_event_when_extension_throws_exception_during_load()
        {
            var directory = Create.EmptyWorkspace().Directory;
            var extensionDll = await KernelExtensionTestHelper.CreateExtension(directory, @"throw new Exception();");

            var kernel = CreateKernel();
            var extensionLoadEvents = new List<IKernelEvent>();
            await new KernelExtensionLoader().LoadFromAssembly(extensionDll, kernel, (kernelEvent) => extensionLoadEvents.Add(kernelEvent));

            extensionLoadEvents.Should()
                      .ContainSingle(e => e is KernelExtensionLoadException);
        }
    }
}