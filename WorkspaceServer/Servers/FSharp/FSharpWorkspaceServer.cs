using System;
using System.IO;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.DotNet.Try.Project;
using Microsoft.DotNet.Try.Protocol;
using WorkspaceServer.Packaging;
using WorkspaceServer.Servers.Roslyn;
using WorkspaceServer.Transformations;
using DiagnosticSeverity = Microsoft.DotNet.Try.Protocol.DiagnosticSeverity;
using File = System.IO.File;
using Package = WorkspaceServer.Packaging.Package;
using Workspace = Microsoft.DotNet.Try.Protocol.Workspace;

namespace WorkspaceServer.Servers.FSharp
{
    public partial class FSharpWorkspaceServer : IWorkspaceServer
    {
        private readonly IPackageFinder _packageFinder;
        private readonly IWorkspaceTransformer _transformer = new FSharpBufferInliningTransformer();

        public FSharpWorkspaceServer(IPackageFinder packageRegistry)
        {
            _packageFinder = packageRegistry ?? throw new ArgumentNullException(nameof(packageRegistry));
        }

        public async Task<CompileResult> Compile(WorkspaceRequest request, Budget budget = null)
        {
            var workspace = request.Workspace;
            var package = await _packageFinder.Find<Package>(workspace.WorkspaceType);
            var (packageWithChanges, compileResult) = await Compile(package, workspace, request.RequestId);
            using (packageWithChanges)
            {
                return compileResult;
            }
        }

        public Task<CompletionResult> GetCompletionList(WorkspaceRequest request, Budget budget = null)
        {
            // TODO:
            return Task.FromResult(new CompletionResult());
        }

        public async Task<DiagnosticResult> GetDiagnostics(WorkspaceRequest request, Budget budget = null)
        {
            //var workspace = request.Workspace;
            //var package = await _packageFinder.Find<Package>(workspace.WorkspaceType);
            //workspace = await _transformer.TransformAsync(workspace);
            //var packageWithChanges = await CreatePackageWithChanges(package, workspace);
            //var packageFiles = packageWithChanges.GetFiles();
            //var diagnostics = await Shim.GetDiagnostics(packageWithChanges.Name, packageFiles, packageWithChanges.Directory.FullName, package.Directory.FullName);
            //var serializableDiagnostics = workspace.MapDiagnostics(request.ActiveBufferId, diagnostics, budget).DiagnosticsInActiveBuffer;
            //return new DiagnosticResult(serializableDiagnostics, request.RequestId);

            throw new NotImplementedException();
        }

        public Task<SignatureHelpResult> GetSignatureHelp(WorkspaceRequest request, Budget budget = null)
        {
            // TODO:
            return Task.FromResult(new SignatureHelpResult());
        }

        public async Task<RunResult> Run(WorkspaceRequest request, Budget budget = null)
        {
            var workspace = request.Workspace;
            var package = await _packageFinder.Find<Package>(workspace.WorkspaceType);
            workspace = await _transformer.TransformAsync(workspace);
            var (packageWithChanges, _) = await Compile(package, workspace, request.RequestId);
            using (packageWithChanges)
            {
                return await RoslynWorkspaceServer.RunConsoleAsync(
                    packageWithChanges,
                    new SerializableDiagnostic[] { },
                    budget,
                    request.RequestId,
                    workspace.IncludeInstrumentation,
                    request.RunArgs);
            }
        }

        private static async Task<RedirectedPackage> CreatePackageWithChanges(Package package, Workspace workspace)
        {
            // copy project and assets to temporary location
            var tempDirName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var packageWithChanges = new RedirectedPackage(workspace, package, Directory.CreateDirectory(tempDirName));
            try
            {
                await CopyDirectory(package.Directory.FullName, packageWithChanges.Directory.FullName);

                // overwrite files
                foreach (var file in workspace.Files)
                {
                    File.WriteAllText(Path.Combine(packageWithChanges.Directory.FullName, Path.GetFileName(file.Name)), file.Text);
                }

                return packageWithChanges;
            }
            catch
            {
                packageWithChanges.Clean();
                return null;
            }
        }

        private async Task<(RedirectedPackage, CompileResult)> Compile(Package package, Workspace workspace, string requestId)
        {
            var packageWithChanges = await CreatePackageWithChanges(package, workspace);
            try
            {
                await package.FullBuild(); // ensure `package.EntryPointAssemblyPath.FullName` has a value
                await packageWithChanges.FullBuild();

                // copy the entire output directory back
                await CopyDirectory(
                    Path.GetDirectoryName(packageWithChanges.EntryPointAssemblyPath.FullName),
                    Path.GetDirectoryName(package.EntryPointAssemblyPath.FullName));

                return (packageWithChanges, new CompileResult(
                    true, // succeeded
                    Convert.ToBase64String(File.ReadAllBytes(package.EntryPointAssemblyPath.FullName)),
                    diagnostics: null,
                    requestId: requestId));
            }
            catch (Exception e)
            {
                packageWithChanges.Clean();
                return (null, new CompileResult(
                    false, // succeeded
                    string.Empty, // assembly base64
                    new SerializableDiagnostic[]
                    {
                        // TODO: populate with real compiler diagnostics
                        new SerializableDiagnostic(0, 0, e.Message, DiagnosticSeverity.Error, "Compile error")
                    },
                    requestId));
            }
        }

        private static async Task CopyDirectory(string source, string destination)
        {
            foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dir.Replace(source, destination));
            }

            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var attempt = 0;
                var totalAttempts = 100;
                try
                {
                    File.Copy(file, file.Replace(source, destination), true);
                }
                catch (IOException)
                {
                    if (attempt++ == totalAttempts)
                    {
                        throw;
                    }

                    await Task.Delay(10);
                }
            }
        }
    }
}
