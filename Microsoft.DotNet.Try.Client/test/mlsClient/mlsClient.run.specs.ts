// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import IMlsClient from "../../src/IMlsClient";
import chai = require("chai");
import { IWorkspace } from "../../src/IState";
import ICanGetAClient from "./ICanGetAClient";

chai.use(require("chai-as-promised"));
chai.use(require("chai-subset"));
chai.should();

function createScriptWorkspace(sourceCode:string) : IWorkspace{
    let ws: IWorkspace = {
        usings: [],
        workspaceType: "script",
        files: [],
        buffers: [{ position: 0, id: "Program.cs", content: sourceCode }]
    };
    return ws;
}

export default (getClient: ICanGetAClient) => {
    let client: IMlsClient;

    describe("getCompileAndExecuteResponse", () => {
        beforeEach(async function () {
            client = await getClient();
        });

        it("returns output for successfuly executed request", async function () {
            let sourceCode = "Console.WriteLine(\"Hello, World\");";

            let ws = createScriptWorkspace(sourceCode);
            let result = await client.run({ workspace: ws });

            result.succeeded.should.equal(true);

            result.output.should.deep.equal(["Hello, World"]);
        });

        it("returns exception for request that throws an exception", async function () {
            let sourceCode = "throw new Exception(\"Goodbye, World\");";
            let ws = createScriptWorkspace(sourceCode);
            let result = await client.run({ workspace: ws });

            result.succeeded.should.equal(true);

            result.exception.should.contain("System.Exception: Goodbye, World");
        });

        it("returns output for request that throws an exception", async function () {
            let sourceCode = "Console.WriteLine(\"Hello, World\");throw new Exception(\"Goodbye, World\");";
            let ws = createScriptWorkspace(sourceCode);
            let result = await client.run({ workspace: ws });

            result.succeeded.should.equal(true);

            result.output.should.deep.equal(["Hello, World"]);
        });

        it("returns diagnostics for request with syntax errors", async function () {
            let sourceCode = "Console.PrintLine();";
            let ws = createScriptWorkspace(sourceCode);
            let result = await client.run({ workspace: ws });

            result.succeeded.should.equal(false);

            result.diagnostics.should.deep.equal([{
                start: 8,
                end: 17,
                message: "'Console' does not contain a definition for 'PrintLine'",
                severity: 3,
                id: "CS0117"
            }]);
        });
    });
};
