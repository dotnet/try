// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import IMlsClient, { IGistWorkspace } from "../../src/IMlsClient";
import { suite } from "mocha-typescript";

import chai = require("chai");
import { createMockHttpServer, IMockHttpServer } from "./mockHttpServerFactory";
import ICanGetAClient from "./ICanGetAClient";


chai.use(require("chai-as-promised"));
chai.use(require("chai-subset"));
chai.should();

export default (getClient: ICanGetAClient) => {
    suite(`getWorkspaceFromGist`, () => {
        let server: IMockHttpServer;

        let client: IMlsClient;

        beforeEach(async function () {
            server = await createMockHttpServer("localhost");
            await server.start();
            client = await getClient();
        });

        it("can retrieve source code from url", async function () {

            const expectedGist: IGistWorkspace = {
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
                originType: "gist",
                workspace: {
                    buffers: [{
                        id: "Program.cs",
                        content: "using System;\nusing Newtonsoft.Json;\nusing Newtonsoft.Json.Serialization;\nusing Newtonsoft.Json.Converters;\nusing Newtonsoft.Json.Linq;\n\nnamespace jsonDotNetExperiment\n{\n    class Program\n    {\n        static void Main(string[] args)\n        {\n            Console.WriteLine(\"jsonDotNet workspace\");\n            #region jsonSnippet\n            var simpleObject = new JObject\n            {\n                {\"property\", 4}\n            };\n            Console.WriteLine(simpleObject.ToString(Formatting.Indented));\n            #endregion\n            Console.WriteLine(\"Bye!\");\n            Console.WriteLine(\"Bye!\");\n        }\n    }\n}",
                        position: 0
                    }, {
                        id: "secondFile.cs",
                        content: "using System;\nusing Newtonsoft.Json;\nusing Newtonsoft.Json.Serialization;\nusing Newtonsoft.Json.Converters;\nusing Newtonsoft.Json.Linq;\n\nnamespace jsonDotNetExperiment\n{\n    class ProgramTwo\n    {\n        static void Main(string[] args)\n        {\n            Console.WriteLine(\"jsonDotNet workspace\");\n            #region jsonSnippet\n            var simpleObject = new JObject\n            {\n                {\"property\", 4}\n            };\n            Console.WriteLine(simpleObject.ToString(Formatting.Indented));\n            #endregion\n            Console.WriteLine(\"Bye!\");\n            Console.WriteLine(\"Bye!\");\n        }\n    }\n}",
                        position: 0
                    }, {
                        id: "thirdFile.cs",
                        content: "using System;\nusing Newtonsoft.Json;\nusing Newtonsoft.Json.Serialization;\nusing Newtonsoft.Json.Converters;\nusing Newtonsoft.Json.Linq;\n\nnamespace jsonDotNetExperiment\n{\n    class ProgramThree\n    {\n        static void Main(string[] args)\n        {\n            Console.WriteLine(\"jsonDotNet workspace\");\n            #region jsonSnippet\n            var simpleObject = new JObject\n            {\n                {\"property\", 4}\n            };\n            Console.WriteLine(simpleObject.ToString(Formatting.Indented));\n            #endregion\n            Console.WriteLine(\"Bye!\");\n            Console.WriteLine(\"Bye!\");\n        }\n    }\n}",
                        position: 0
                    }],
                    usings: [],
                    workspaceType: "console",
                    files: []
                }
            };
            server.on({
                method: "GET",
                path: "/workspace/fromgist/3d5c3795a58b3e9345e44b5a4541a9c7/d1b537520d812de49ae5639ca487d3e99304a488",
                reply: {
                    status: 200,
                    body: function () {
                        return expectedGist;
                    }
                }
            });


            let result = await client.getWorkspaceFromGist("3d5c3795a58b3e9345e44b5a4541a9c7/d1b537520d812de49ae5639ca487d3e99304a488", "console", false);

            result.originType.should.be.equal("gist");
            result.workspace.workspaceType.should.be.equal("console");

        });

        afterEach(async function () {
            await server.stop();
        });
    });
};
