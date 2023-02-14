// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { describe } from "mocha";

import * as polyglotNotebooks from "@microsoft/polyglot-notebooks";
import * as CSharpProjectKernelWithWASMRunner from "../src/ProjectKernelWithWASMRunner";
import { createApiServiceSimulator } from "./apiServiceSimulator";
import { createWasmRunnerSimulator } from "./wasmRunnerSimulator";

describe("Project kernel", () => {
    beforeEach(() => {
        polyglotNotebooks.Logger.configure("debug", (entry) => {
            //  console.log(entry.message);
        });
    });

    it("cannot open document if there is no open project", async () => {
        let service = createApiServiceSimulator();
        let wasmRunner = createWasmRunnerSimulator();
        let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);

        let eventEnvelopes: polyglotNotebooks.KernelEventEnvelope[] = [];
        kernel.subscribeToKernelEvents(e => {
            eventEnvelopes.push(e);
        });
        await kernel.send({ commandType: polyglotNotebooks.OpenDocumentType, command: <polyglotNotebooks.OpenDocument>{ relativeFilePath: "./Program.cs" } });

        let commandFailed = eventEnvelopes.find(e => e.eventType === polyglotNotebooks.CommandFailedType);
        expect(commandFailed).not.to.be.undefined;
        expect((<polyglotNotebooks.CommandFailed>(commandFailed!.event)).message).to.equal("Project must be opened, send the command 'OpenProject' first.");
    });

    it("cannot request diagnostics if there is no open document", async () => {
        let service = createApiServiceSimulator("./simulatorConfigurations/apiService/open_project.json");
        let wasmRunner = createWasmRunnerSimulator();
        let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);

        await openProject(
            kernel,
            {
                files: [{
                    relativeFilePath: "./Program.cs",
                    content: "\npublic class Program\n{\n    public static void Main(string[] args)\n    {\n        #region REGION_1\n        var a = 123;\n        #endregion\n\n        #region REGION_2\n        var b = 123;\n        #endregion\n    }\n}"
                }]
            });

        let eventEnvelopes: polyglotNotebooks.KernelEventEnvelope[] = [];
        kernel.subscribeToKernelEvents(e => {
            eventEnvelopes.push(e);
        });
        await kernel.send({ commandType: polyglotNotebooks.RequestDiagnosticsType, command: <polyglotNotebooks.RequestDiagnostics>{ code: "Console.WriteLine(1);" } });

        let commandFailed = eventEnvelopes.find(e => e.eventType === polyglotNotebooks.CommandFailedType);
        expect(commandFailed).not.to.be.undefined;
        expect((<polyglotNotebooks.CommandFailed>(commandFailed!.event)).message).to.equal("Document must be opened, send the command 'OpenDocument' first.");
    });

    it("cannot request completions if there is no open document", async () => {
        let service = createApiServiceSimulator("./simulatorConfigurations/apiService/open_project.json");
        let wasmRunner = createWasmRunnerSimulator();
        let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);

        await openProject(
            kernel,
            {
                files: [{
                    relativeFilePath: "./Program.cs",
                    content: "\npublic class Program\n{\n    public static void Main(string[] args)\n    {\n        #region REGION_1\n        var a = 123;\n        #endregion\n\n        #region REGION_2\n        var b = 123;\n        #endregion\n    }\n}"
                }]
            });

        let eventEnvelopes: polyglotNotebooks.KernelEventEnvelope[] = [];

        kernel.subscribeToKernelEvents(e => {
            eventEnvelopes.push(e);
        });

        await kernel.send({ commandType: polyglotNotebooks.RequestCompletionsType, command: <polyglotNotebooks.RequestCompletions>{ code: "Console.WriteLine(1);", linePosition: { character: 1, line: 1 } } });

        let commandFailed = eventEnvelopes.find(e => e.eventType === polyglotNotebooks.CommandFailedType);
        expect(commandFailed).not.to.be.undefined;
        expect((<polyglotNotebooks.CommandFailed>(commandFailed!.event)).message).to.equal("Document must be opened, send the command 'OpenDocument' first.");
    });

    it("cannot request signaturehelp if there is no open document", async () => {
        let service = createApiServiceSimulator("./simulatorConfigurations/apiService/open_project.json");
        let wasmRunner = createWasmRunnerSimulator();
        let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);

        await openProject(
            kernel,
            {
                files: [{
                    relativeFilePath: "./Program.cs",
                    content: "\npublic class Program\n{\n    public static void Main(string[] args)\n    {\n        #region REGION_1\n        var a = 123;\n        #endregion\n\n        #region REGION_2\n        var b = 123;\n        #endregion\n    }\n}"
                }]
            });

        let eventEnvelopes: polyglotNotebooks.KernelEventEnvelope[] = [];
        kernel.subscribeToKernelEvents(e => {
            eventEnvelopes.push(e);
        });
        await kernel.send({ commandType: polyglotNotebooks.RequestSignatureHelpType, command: <polyglotNotebooks.RequestSignatureHelp>{ code: "Console.WriteLine(1);", linePosition: { character: 1, line: 1 } } });

        let commandFailed = eventEnvelopes.find(e => e.eventType === polyglotNotebooks.CommandFailedType);
        expect(commandFailed).not.to.be.undefined;
        expect((<polyglotNotebooks.CommandFailed>(commandFailed!.event)).message).to.equal("Document must be opened, send the command 'OpenDocument' first.");
    });

    it("cannot request hovertext if there is no open document", async () => {
        let service = createApiServiceSimulator("./simulatorConfigurations/apiService/open_project.json");
        let wasmRunner = createWasmRunnerSimulator();
        let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);

        await openProject(
            kernel,
            {
                files: [{
                    relativeFilePath: "./Program.cs",
                    content: "\npublic class Program\n{\n    public static void Main(string[] args)\n    {\n        #region REGION_1\n        var a = 123;\n        #endregion\n\n        #region REGION_2\n        var b = 123;\n        #endregion\n    }\n}"
                }]
            });

        let eventEnvelopes: polyglotNotebooks.KernelEventEnvelope[] = [];
        kernel.subscribeToKernelEvents(e => {
            eventEnvelopes.push(e);
        });
        await kernel.send({ commandType: polyglotNotebooks.RequestHoverTextType, command: <polyglotNotebooks.RequestHoverText>{ code: "Console.WriteLine(1);", linePosition: { character: 1, line: 1 } } });

        let commandFailed = eventEnvelopes.find(e => e.eventType === polyglotNotebooks.CommandFailedType);
        expect(commandFailed).not.to.be.undefined;
        expect((<polyglotNotebooks.CommandFailed>(commandFailed!.event)).message).to.equal("Document must be opened, send the command 'OpenDocument' first.");
    });

    it("cannot submitCode if there is no open document", async () => {
        let service = createApiServiceSimulator("./simulatorConfigurations/apiService/open_project.json");
        let wasmRunner = createWasmRunnerSimulator();
        let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);

        await openProject(
            kernel,
            {
                files: [{
                    relativeFilePath: "./Program.cs",
                    content: "\npublic class Program\n{\n    public static void Main(string[] args)\n    {\n        #region REGION_1\n        var a = 123;\n        #endregion\n\n        #region REGION_2\n        var b = 123;\n        #endregion\n    }\n}"
                }]
            });

        let eventEnvelopes: polyglotNotebooks.KernelEventEnvelope[] = [];
        kernel.subscribeToKernelEvents(e => {
            eventEnvelopes.push(e);
        });
        await kernel.send({ commandType: polyglotNotebooks.SubmitCodeType, command: <polyglotNotebooks.SubmitCode>{ code: "Console.WriteLine(1);" } });

        let commandFailed = eventEnvelopes.find(e => e.eventType === polyglotNotebooks.CommandFailedType);
        expect(commandFailed).not.to.be.undefined;
        expect((<polyglotNotebooks.CommandFailed>(commandFailed!.event)).message).to.equal("Document must be opened, send the command 'OpenDocument' first.");
    });

    it("when opening a project it produces the project manifest", async () => {
        let service = createApiServiceSimulator("./simulatorConfigurations/apiService/open_project.json");
        let wasmRunner = createWasmRunnerSimulator();
        let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);

        let eventEnvelopes: polyglotNotebooks.KernelEventEnvelope[] = [];
        kernel.subscribeToKernelEvents(e => {
            eventEnvelopes.push(e);
        });
        await kernel.send({
            commandType: polyglotNotebooks.OpenProjectType, command: <polyglotNotebooks.OpenProject>{
                project: {
                    files: [{
                        relativeFilePath: "./Program.cs",
                        content: "\npublic class Program\n{\n    public static void Main(string[] args)\n    {\n        #region REGION_1\n        var a = 123;\n        #endregion\n\n        #region REGION_2\n        var b = 123;\n        #endregion\n    }\n}"
                    }]
                }
            }
        });

        let projectOpened = eventEnvelopes.find(e => e.eventType === polyglotNotebooks.ProjectOpenedType)?.event as polyglotNotebooks.ProjectOpened;
        expect(projectOpened).not.to.be.undefined;
        expect(projectOpened.projectItems).to.not.be.empty;
        expect(projectOpened.projectItems).to.deep.equal([{
            regionNames: ['REGION_1', 'REGION_2'],
            regionsContent: { REGION_1: 'var a = 123;', REGION_2: 'var b = 123;' },
            relativeFilePath: './Program.cs'
        }]);
    });

    it("when opening a document it produces documentOpen event with content", async () => {
        let service = createApiServiceSimulator("./simulatorConfigurations/apiService/open_document_with_region.json");
        let wasmRunner = createWasmRunnerSimulator();
        let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);

        await openProject(
            kernel,
            {
                "files": [
                    {
                        "relativeFilePath": "./Program.cs",
                        "content": "\npublic class Program\n{\n    public static void Main(string[] args)\n    {\n        #region REGION_1\n        var a = 123;\n        #endregion\n\n        #region REGION_2\n        var b = 123;\n        #endregion\n    }\n}"
                    }
                ]
            });

        let eventEnvelopes: polyglotNotebooks.KernelEventEnvelope[] = [];
        kernel.subscribeToKernelEvents(e => {
            eventEnvelopes.push(e);
        });
        await kernel.send({ commandType: polyglotNotebooks.OpenDocumentType, command: <polyglotNotebooks.OpenDocument>{ relativeFilePath: "./Program.cs", regionName: "REGION_2" } });

        let documentOpen = eventEnvelopes.find(e => e.eventType === polyglotNotebooks.DocumentOpenedType)?.event as polyglotNotebooks.DocumentOpened;
        expect(documentOpen).not.to.be.undefined;
        expect(documentOpen.relativeFilePath).to.equal("./Program.cs");
        expect(documentOpen.content).to.equal("var b = 123;");
    });

    it("produces diagnostics for the open document", async () => {
        let service = createApiServiceSimulator("./simulatorConfigurations/apiService/diagnostics_produced_with_errors_in_code.json");
        let wasmRunner = createWasmRunnerSimulator();
        let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);

        await openProjectAndDocument(
            kernel,
            {
                files: [{
                    "relativeFilePath": "./Program.cs",
                    "content": "\npublic class Program\n{\n    public static void Main(string[] args)\n    {\n        int someInt = 1;\n        #region test-region\n        #endregion\n    }\n}\n"
                }]
            },
            "./Program.cs",
            "test-region");

        let eventEnvelopes: polyglotNotebooks.KernelEventEnvelope[] = [];
        kernel.subscribeToKernelEvents(e => {
            eventEnvelopes.push(e);
        });

        await kernel.send({ commandType: polyglotNotebooks.RequestDiagnosticsType, command: <polyglotNotebooks.RequestDiagnostics>{ code: "someInt = \"NaN\";" } });

        let diagnostics = eventEnvelopes.find(e => e.eventType === polyglotNotebooks.DiagnosticsProducedType)?.event as polyglotNotebooks.DiagnosticsProduced;
        expect(diagnostics).not.to.be.undefined;
        expect(diagnostics.diagnostics.length).to.be.greaterThan(0);
        expect(diagnostics.diagnostics.find(d => d.severity === 'error')).to.deep.equal({
            code: 'CS0029',
            linePositionSpan:
            {
                end: { character: 15, line: 0 },
                start: { character: 10, line: 0 }
            },
            message: '(1,11): error CS0029: Cannot implicitly convert type \'string\' to \'int\'',
            severity: 'error'
        });
    });

    it("executes correct code", async () => {
        let service = createApiServiceSimulator("./simulatorConfigurations/apiService/compiles_with_no_warning.json");
        let wasmRunner = createWasmRunnerSimulator("./simulatorConfigurations/wasmRunner/executes_correct_code.json");
        let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);

        await openProjectAndDocument(
            kernel,
            {
                files: [{
                    "relativeFilePath": "./Program.cs",
                    "content": "\npublic class Program\n{\n    public static void Main(string[] args)\n    {\n        #region test-region\n        #endregion\n    }\n}\n"
                }]
            },
            "./Program.cs",
            "test-region");

        let eventEnvelopes: polyglotNotebooks.KernelEventEnvelope[] = [];
        kernel.subscribeToKernelEvents(e => {
            eventEnvelopes.push(e);
        });

        await kernel.send({ commandType: polyglotNotebooks.SubmitCodeType, command: <polyglotNotebooks.SubmitCode>{ code: "System.Console.WriteLine(2);" } });

        let standardOutputValueProduced = eventEnvelopes.find(e => e.eventType === polyglotNotebooks.StandardOutputValueProducedType)?.event as polyglotNotebooks.StandardOutputValueProduced;
        expect(standardOutputValueProduced).not.to.be.undefined;
        expect(standardOutputValueProduced.formattedValues[0].value).to.equal("2");

    });
});

export function openProject(kernel: polyglotNotebooks.Kernel, project: polyglotNotebooks.Project): Promise<void> {
    return kernel.send({
        commandType: polyglotNotebooks.OpenProjectType,
        command: <polyglotNotebooks.OpenProject>{
            project: project
        }
    });
}

export async function openProjectAndDocument(kernel: polyglotNotebooks.Kernel, project: polyglotNotebooks.Project, relativeFilePath: string, regionName?: string): Promise<void> {
    await openProject(kernel, project);

    await kernel.send({
        commandType: polyglotNotebooks.OpenDocumentType,
        command: <polyglotNotebooks.OpenDocument>{
            relativeFilePath: relativeFilePath,
            regionName: regionName
        }
    });
}