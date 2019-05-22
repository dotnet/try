// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import IMlsClient from "../../src/IMlsClient";
import { expect } from "chai";
import { suite } from "mocha-typescript";
import { IWorkspace } from "../../src/IState";
import ICanGetAClient from "./ICanGetAClient";

let chai = require("chai");
chai.use(require("chai-as-promised"));
chai.use(require("chai-subset"));
chai.use(require("chai-exclude"));
chai.should();

export default (getClient: ICanGetAClient) => {
    let client: IMlsClient;

    suite(`getCompletionList`, () => {
        beforeEach(async function () {
            client = await getClient();
        });

        it("returns completion items for completable roslyn request", async function () {
            let sourceCode = "Console.";
            let activeBuffer = "program.cs";
            let ws : IWorkspace = {
                workspaceType: "script",
                buffers: [{id:activeBuffer, content: sourceCode, position:0}]
            };
            let result = await client.getCompletionList(ws, activeBuffer, 8, "roslyn");
            
            // tslint:disable-next-line:no-unused-expression-chai
            result.items.should.not.be.empty;

            result.items.slice(0, 5).should.containSubset(RoslynConsoleCompletions);
        });

        it("returns diagnostics for completable roslyn request", async function () {
            let sourceCode = "Console.";
            let activeBuffer = "program.cs";
            let ws : IWorkspace = {
                workspaceType: "script",
                buffers: [{id:activeBuffer, content: sourceCode, position:0}]
            };
            let result = await client.getCompletionList(ws, activeBuffer, 8, "roslyn");
            
            // tslint:disable-next-line:no-unused-expression-chai
            result.diagnostics.should.not.be.empty;
        });


        it("returns completion items with acceptanceUri for completable pythia request", async function () {
              let sourceCode = "Console.";
            let activeBuffer = "program.cs";
            let ws : IWorkspace = {
                workspaceType: "script",
                buffers: [{id:activeBuffer, content: sourceCode, position:0}]
            };
            let result = await client.getCompletionList(ws, activeBuffer, 8, "pythia");

            result.items.slice(0, 5).should.excluding("acceptanceUri").deep.equal(PythiaConsoleCompletions);

            for (let item of result.items.slice(0, 5)) {
                expect(item.acceptanceUri).to.not.be.null;
            }
        });
    });
};

let RoslynConsoleCompletions = [{
    filterText: "BackgroundColor",
    insertText: "BackgroundColor",
    kind: 9,
    label: "BackgroundColor",
    sortText: "BackgroundColor"
},
{
    filterText: "Beep",
    insertText: "Beep",
    kind: 1,
    label: "Beep",
    sortText: "Beep"
},
{
    filterText: "BufferHeight",
    insertText: "BufferHeight",
    kind: 9,
    label: "BufferHeight",
    sortText: "BufferHeight"
},
{
    filterText: "BufferWidth",
    insertText: "BufferWidth",
    kind: 9,
    label: "BufferWidth",
    sortText: "BufferWidth"
},
{
    filterText: "CancelKeyPress",
    insertText: "CancelKeyPress",
    kind: 9,
    label: "CancelKeyPress",
    sortText: "CancelKeyPress"
}];

let PythiaConsoleCompletions = [{
    documentation: {
        value: "Writes the current line terminator to the standard output stream.\nSystem.IO.IOException: An I/O error occurred.",
        isTrusted: false
    },
    filterText: "WriteLine",
    insertText: "WriteLine",
    label: "★ WriteLine",
    kind: 1,
    sortText: "0"
},
{
    documentation: {
        value: "Gets or sets the background color of the console.\nReturns: A value that specifies the background color of the console; that is, the color that appears behind each character. The default is black.\nSystem.ArgumentException: The color specified in a set operation is not a valid member of System.ConsoleColor .\nSystem.Security.SecurityException: The user does not have permission to perform this action.\nSystem.IO.IOException: An I/O error occurred.",
        isTrusted: false
    },
    filterText: "BackgroundColor",
    insertText: "BackgroundColor",
    kind: 9,
    label: "BackgroundColor",
    sortText: "BackgroundColor"
},
{
    documentation: {
        value: "Plays the sound of a beep through the console speaker.\nSystem.Security.HostProtectionException: This method was executed on a server, such as SQL Server, that does not permit access to a user interface.",
        isTrusted: false
    },
    filterText: "Beep",
    insertText: "Beep",
    kind: 1,
    label: "Beep",
    sortText: "Beep"
},
{
    documentation: {
        value: "Gets or sets the height of the buffer area.\nReturns: The current height, in rows, of the buffer area.\nSystem.ArgumentOutOfRangeException: The value in a set operation is less than or equal to zero.   -or-   The value in a set operation is greater than or equal to System.Int16.MaxValue .   -or-   The value in a set operation is less than System.Console.WindowTop + System.Console.WindowHeight .\nSystem.Security.SecurityException: The user does not have permission to perform this action.\nSystem.IO.IOException: An I/O error occurred.",
        isTrusted: false
    },
    filterText: "BufferHeight",
    insertText: "BufferHeight",
    kind: 9,
    label: "BufferHeight",
    sortText: "BufferHeight"
},
{
    documentation: {
        value: "Gets or sets the width of the buffer area.\nReturns: The current width, in columns, of the buffer area.\nSystem.ArgumentOutOfRangeException: The value in a set operation is less than or equal to zero.   -or-   The value in a set operation is greater than or equal to System.Int16.MaxValue .   -or-   The value in a set operation is less than System.Console.WindowLeft + System.Console.WindowWidth .\nSystem.Security.SecurityException: The user does not have permission to perform this action.\nSystem.IO.IOException: An I/O error occurred.",
        isTrusted: false
    },
    filterText: "BufferWidth",
    insertText: "BufferWidth",
    kind: 9,
    label: "BufferWidth",
    sortText: "BufferWidth"
}];
