// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IWorkspace } from "../src/IState";
import { ClientConfiguration } from "../src/clientConfiguration";
import { Project } from "../src/clientApiProtocol";

export const fibonacciCode = `using System;
using System.Collections.Generic;
using System.Linq;

public class Program
{
  public static void Main()
  {
    foreach (var i in Fibonacci().Take(20))
    {
      Console.WriteLine(i);
    }
  }

  private static IEnumerable<int> Fibonacci()
  {
    int current = 1, next = 1;

    while (true) 
    {
      yield return current;
      next = current + (current = next);
    }
  }
}
`;

export const emptyWorkspace: IWorkspace = {
  workspaceType: "script",
  files: [],
  buffers: [{ id: "Program.cs", content: "", position: 0 }],
  usings: [],
  language:"csharp"
};

export const defaultWorkspace: IWorkspace = {
  workspaceType: "script",
  files: [],
  buffers: [{ id: "Program.cs", content: fibonacciCode, position: 0 }],
  usings: [],
};

export const defaultGistProject: Project = {
  projectTemplate: "console",
  files: [{
    name: "Program.cs",
    content: "using System;\nusing Newtonsoft.Json;\nusing Newtonsoft.Json.Serialization;\nusing Newtonsoft.Json.Converters;\nusing Newtonsoft.Json.Linq;\n\nnamespace jsonDotNetExperiment\n{\n    class Program\n    {\n        static void Main(string[] args)\n        {\n            Console.WriteLine(\"jsonDotNet workspace\");\n            #region jsonSnippet\n            var simpleObject = new JObject\n            {\n                {\"property\", 4}\n            };\n            Console.WriteLine(simpleObject.ToString(Formatting.Indented));\n            #endregion\n            Console.WriteLine(\"Bye!\");\n            Console.WriteLine(\"Bye!\");\n        }\n    }\n}"
  }, {
    name: "secondFile.cs",
    content: "using System;\nusing Newtonsoft.Json;\nusing Newtonsoft.Json.Serialization;\nusing Newtonsoft.Json.Converters;\nusing Newtonsoft.Json.Linq;\n\nnamespace jsonDotNetExperiment\n{\n    class ProgramTwo\n    {\n        static void Main(string[] args)\n        {\n            Console.WriteLine(\"jsonDotNet workspace\");\n            #region jsonSnippet\n            var simpleObject = new JObject\n            {\n                {\"property\", 4}\n            };\n            Console.WriteLine(simpleObject.ToString(Formatting.Indented));\n            #endregion\n            Console.WriteLine(\"Bye!\");\n            Console.WriteLine(\"Bye!\");\n        }\n    }\n}"
  }, {
    name: "thirdFile.cs",
    content: "using System;\nusing Newtonsoft.Json;\nusing Newtonsoft.Json.Serialization;\nusing Newtonsoft.Json.Converters;\nusing Newtonsoft.Json.Linq;\n\nnamespace jsonDotNetExperiment\n{\n    class ProgramThree\n    {\n        static void Main(string[] args)\n        {\n            Console.WriteLine(\"jsonDotNet workspace\");\n            #region jsonSnippet\n            var simpleObject = new JObject\n            {\n                {\"property\", 4}\n            };\n            Console.WriteLine(simpleObject.ToString(Formatting.Indented));\n            #endregion\n            Console.WriteLine(\"Bye!\");\n            Console.WriteLine(\"Bye!\");\n        }\n    }\n}"
  }],
  htmlUrl: "https://gist.github.com/3d5c3795a58b3e9345e44b5a4541a9c7",
  rawFileUrls: [{
    fileName: "Program.cs",
    url: "https://gist.githubusercontent.com/colombod/3d5c3795a58b3e9345e44b5a4541a9c7/raw/894dbbd89a23bcad1d997b1bbe386a246a0f8c95/Program.cs"
  }, {
    fileName: "secondFile.cs",
    url: "https://gist.githubusercontent.com/colombod/3d5c3795a58b3e9345e44b5a4541a9c7/raw/ed8c5a7f32f00ac26697f0c6dec648651d8c2aeb/secondFile.cs"
  }, {
    fileName: "thirdFile.cs",
    url: "https://gist.githubusercontent.com/colombod/3d5c3795a58b3e9345e44b5a4541a9c7/raw/6f6e2fe5d0b6a26db0071eac3ac79d57d24f029/thirdFile.cs"
  }],
  projectOriginType: "gist"
};



export const clientConfigurationExample: ClientConfiguration = {
  versionId: "A5F0CCD0-FE6F-4E5A-9E18-2CFA508F8423",
  enableBranding: true,
  applicationInsightsKey: "not-a-key",
  defaultTimeoutMs: 15000,
  _links: {
    _self: {
      timeoutMs: 15000,
      href: "/clientConfiguration",
      templated: false,
      method: "POST",
      body: <any>""
    },
    configuration: {
      timeoutMs: 15000,
      href: "/clientConfiguration",
      templated: false,
      method: "POST"
    },
    completion: {
      timeoutMs: 15000,
      href: "/workspace/completion",
      templated: false,
      method: "POST",
      properties: [{
        name: "completionProvider"
      }]
    },
    acceptCompletion: {
      timeoutMs: 15000,
      href: "{acceptanceUri}",
      templated: true,
      method: "POST"
    },
    loadFromGist: {
      timeoutMs: 15000,
      href: "/workspace/fromgist/{gistId}/{commitHash?}",
      templated: true,
      properties: [{
        name: "workspaceType"
      }, {
        name: "extractBuffers"
      }],
      method: "GET"
    },
    diagnostics: {
      timeoutMs: 15000,
      href: "/workspace/diagnostics",
      templated: false,
      method: "POST"
    },
    signatureHelp: {
      timeoutMs: 15000,
      href: "/workspace/signatureHelp",
      templated: false,
      method: "POST"
    },
    run: {
      timeoutMs: 15000,
      href: "/workspace/run",
      templated: false,
      method: "POST"
    },
    snippet: {
      timeoutMs: 15000,
      href: "/snippet",
      templated: false,
      properties: [{
        name: "from"
      }],
      method: "GET"
    },
    projectFromGist: {
      timeoutMs: 15000,
      href: "/project/fromGist",
      templated: false,
      properties: [],
      method: "POST"
    },
    regionsFromFiles: {
      timeoutMs: 15000,
      href: "/project/files/regions",
      templated: false,
      properties: [],
      method: "POST"
    }
  }
};

