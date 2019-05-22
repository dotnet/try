// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import IMlsClient from "../../src/IMlsClient";
import { suite } from "mocha-typescript";
import chai = require("chai");
import { IWorkspace } from "../../src/IState";
import ICanGetAClient from "./ICanGetAClient";

chai.use(require("chai-as-promised"));
chai.use(require("chai-subset"));
chai.should();

export default (getClient: ICanGetAClient) => {
    let client: IMlsClient;

    suite(`getSignatureHelp`, () => {
        beforeEach(async function () {
            client = await getClient();
        });

        suite(`script workspace`, () => {
            it("returns signature help items for helpable request", async function () {
                let sourceCode = "class C { void Foo(int a) { Foo( )}  }";
                let workSpace: IWorkspace = { workspaceType: "script", buffers: [{ id: "default", content: sourceCode, position: 0 }] };
                let result = await client.getSignatureHelp(workSpace, "default", 32);
                result.signatures.should.deep.equal(signatureHelpSample.signatures);
                result.activeParameter.should.deep.equal(signatureHelpSample.activeParameter);
                result.activeSignature.should.deep.equal(signatureHelpSample.activeSignature);
            });

            it("returns diagnostics for helpable request", async function () {
                let sourceCode = "class C { void Foo(int a) { Foo( )}  }";
                let workSpace: IWorkspace = { workspaceType: "script", buffers: [{ id: "default", content: sourceCode, position: 0 }] };
                let result = await client.getSignatureHelp(workSpace, "default", 32);
                // tslint:disable-next-line:no-unused-expression-chai
                result.diagnostics.should.not.be.empty;
            });
        });
    });
};

let signatureHelpSample = {
    diagnostics: [{
        start: 201,
        end: 201,
        message: "Program.cs(11,29): error CS1002: ; expected",
        severity: 3,
        id: "CS1002"
    }],
    activeParameter: 0,
    activeSignature: 0,
    signatures: [
        {
            name: "Write",
            label: "void Console.Write(bool value)",
            documentation: {
                value: "Writes the text representation of the specified Boolean value to the standard output stream.",
                isTrusted: false
            },
            parameters: [
            {
                name: "value",
                label: "bool value",
                documentation: {
                    value: "**value**: The value to write.",
                    isTrusted: false
                }
            }]
        },
    ]
};
