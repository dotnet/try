using System.Threading.Tasks;
using Clockwise;
using Microsoft.DotNet.Try.Protocol;
using WorkspaceServer.Packaging;
using WorkspaceServer.Servers.FSharp;
using WorkspaceServer.Servers.Roslyn;

namespace WorkspaceServer.Servers
{
    public class WorkspaceServerMultiplexer : IWorkspaceServer
    {
        private IWorkspaceServer _roslynWorkspaceServer;
        private IWorkspaceServer _fsharpWorksapceServer;

        public WorkspaceServerMultiplexer(IPackageFinder packageRegistry)
        {
            _roslynWorkspaceServer = new RoslynWorkspaceServer(packageRegistry);
            _fsharpWorksapceServer = new FSharpWorkspaceServer(packageRegistry);
        }

        public Task<CompileResult> Compile(WorkspaceRequest request, Budget budget = null)
        {
            return IsFSharpWorkspaceRequest(request)
                ? _fsharpWorksapceServer.Compile(request, budget)
                : _roslynWorkspaceServer.Compile(request, budget);
        }

        public Task<CompletionResult> GetCompletionList(WorkspaceRequest request, Budget budget = null)
        {
            return IsFSharpWorkspaceRequest(request)
                ? _fsharpWorksapceServer.GetCompletionList(request, budget)
                : _roslynWorkspaceServer.GetCompletionList(request, budget);
        }

        public Task<DiagnosticResult> GetDiagnostics(WorkspaceRequest request, Budget budget = null)
        {
            return IsFSharpWorkspaceRequest(request)
                ? _fsharpWorksapceServer.GetDiagnostics(request, budget)
                : _roslynWorkspaceServer.GetDiagnostics(request, budget);
        }

        public Task<SignatureHelpResult> GetSignatureHelp(WorkspaceRequest request, Budget budget = null)
        {
            return IsFSharpWorkspaceRequest(request)
                ? _fsharpWorksapceServer.GetSignatureHelp(request, budget)
                : _roslynWorkspaceServer.GetSignatureHelp(request, budget);
        }

        public Task<RunResult> Run(WorkspaceRequest request, Budget budget = null)
        {
            return IsFSharpWorkspaceRequest(request)
                ? _fsharpWorksapceServer.Run(request, budget)
                : _roslynWorkspaceServer.Run(request, budget);
        }

        private bool IsFSharpWorkspaceRequest(WorkspaceRequest request)
        {
            return request.Workspace.WorkspaceType.EndsWith(".fsproj");
        }
    }
}
