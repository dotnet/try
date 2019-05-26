using System.Threading.Tasks;
using Clockwise;
using Microsoft.DotNet.Try.Protocol;
using WorkspaceServer.Packaging;
using WorkspaceServer.Servers.FSharp;
using WorkspaceServer.Servers.Roslyn;
using Package = WorkspaceServer.Packaging.Package;

namespace WorkspaceServer.Servers
{
    public class WorkspaceServerMultiplexer : IWorkspaceServer
    {
        private IPackageFinder _packageFinder;
        private IWorkspaceServer _roslynWorkspaceServer;
        private IWorkspaceServer _fsharpWorksapceServer;

        public WorkspaceServerMultiplexer(IPackageFinder packageFinder)
        {
            _packageFinder = packageFinder;
            _roslynWorkspaceServer = new RoslynWorkspaceServer(packageFinder);
            _fsharpWorksapceServer = new FSharpWorkspaceServer(packageFinder);
        }

        public async Task<CompileResult> Compile(WorkspaceRequest request, Budget budget = null)
        {
            return await IsFSharpWorkspaceRequest(request.Workspace)
                ? await _fsharpWorksapceServer.Compile(request, budget)
                : await _roslynWorkspaceServer.Compile(request, budget);
        }

        public async Task<CompletionResult> GetCompletionList(WorkspaceRequest request, Budget budget = null)
        {
            return await IsFSharpWorkspaceRequest(request.Workspace)
                ? await _fsharpWorksapceServer.GetCompletionList(request, budget)
                : await _roslynWorkspaceServer.GetCompletionList(request, budget);
        }

        public async Task<DiagnosticResult> GetDiagnostics(WorkspaceRequest request, Budget budget = null)
        {
            return await IsFSharpWorkspaceRequest(request.Workspace)
                ? await _fsharpWorksapceServer.GetDiagnostics(request, budget)
                : await _roslynWorkspaceServer.GetDiagnostics(request, budget);
        }

        public async Task<SignatureHelpResult> GetSignatureHelp(WorkspaceRequest request, Budget budget = null)
        {
            return await IsFSharpWorkspaceRequest(request.Workspace)
                ? await _fsharpWorksapceServer.GetSignatureHelp(request, budget)
                : await _roslynWorkspaceServer.GetSignatureHelp(request, budget);
        }

        public async Task<RunResult> Run(WorkspaceRequest request, Budget budget = null)
        {
            return await IsFSharpWorkspaceRequest(request.Workspace)
                ? await _fsharpWorksapceServer.Run(request, budget)
                : await _roslynWorkspaceServer.Run(request, budget);
        }

        private async Task<bool> IsFSharpWorkspaceRequest(Workspace workspace)
        {
            var package = await _packageFinder.Find<Package>(workspace.WorkspaceType);
            return package.Initializer.Language == "F#";
        }
    }
}
