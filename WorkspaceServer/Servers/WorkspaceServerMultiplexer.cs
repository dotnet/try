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
        private readonly IWorkspaceServer _roslynWorkspaceServer;
        private readonly IWorkspaceServer _fsharpWorkspaceServer;

        public WorkspaceServerMultiplexer(IPackageFinder packageFinder)
        {
            _packageFinder = packageFinder;
            _roslynWorkspaceServer = new RoslynWorkspaceServer(packageFinder);
            _fsharpWorkspaceServer = new FSharpWorkspaceServer(packageFinder);
        }

        public async Task<CompileResult> Compile(WorkspaceRequest request, Budget budget = null)
        {
            return IsFSharpWorkspaceRequest(request.Workspace)
                ? await _fsharpWorkspaceServer.Compile(request, budget)
                : await _roslynWorkspaceServer.Compile(request, budget);
        }

        public async Task<CompletionResult> GetCompletionList(WorkspaceRequest request, Budget budget = null)
        {
            return IsFSharpWorkspaceRequest(request.Workspace)
                ? await _fsharpWorkspaceServer.GetCompletionList(request, budget)
                : await _roslynWorkspaceServer.GetCompletionList(request, budget);
        }

        public async Task<DiagnosticResult> GetDiagnostics(WorkspaceRequest request, Budget budget = null)
        {
            return IsFSharpWorkspaceRequest(request.Workspace)
                ? await _fsharpWorkspaceServer.GetDiagnostics(request, budget)
                : await _roslynWorkspaceServer.GetDiagnostics(request, budget);
        }

        public async Task<SignatureHelpResult> GetSignatureHelp(WorkspaceRequest request, Budget budget = null)
        {
            return IsFSharpWorkspaceRequest(request.Workspace)
                ? await _fsharpWorkspaceServer.GetSignatureHelp(request, budget)
                : await _roslynWorkspaceServer.GetSignatureHelp(request, budget);
        }

        public async Task<RunResult> Run(WorkspaceRequest request, Budget budget = null)
        {
            return IsFSharpWorkspaceRequest(request.Workspace)
                ? await _fsharpWorkspaceServer.Run(request, budget)
                : await _roslynWorkspaceServer.Run(request, budget);
        }

        private bool IsFSharpWorkspaceRequest(Workspace workspace)
        {
            return workspace.Language == "fsharp";
        }
    }
}
