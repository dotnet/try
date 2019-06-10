using System;
using System.Collections.Generic;
using System.Text;

namespace WorkspaceServer.Servers
{
    public interface IWorkspaceServer : ILanguageService, ICodeRunner, ICodeCompiler
    {
    }
}
