// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { describe } from "mocha";

import * as dotnetInteractive from "@microsoft/dotnet-interactive";
import * as CSharpProjectKernelWithWASMRunner from "../src/ProjectKernelWithWASMRunner";
import { createApiServiceSimulator } from "./apiServiceSimulator";
import { createWasmRunnerSimulator } from "./wasmRunnerSimulator";

describe("Project kernel", () => {
    beforeEach(() => {
        dotnetInteractive.Logger.configure("debug", (entry) => {
            //  console.log(entry.message);
        });
    });

    it("cannot open document if there is no open project", async () => {
        let service = createApiServiceSimulator();
        let wasmRunner = createWasmRunnerSimulator();
        let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);

        let eventEnvelopes: dotnetInteractive.KernelEventEnvelope[] = [];
        kernel.subscribeToKernelEvents(e => {
            eventEnvelopes.push(e);
        });
        await kernel.send({ commandType: dotnetInteractive.OpenDocumentType, command: <dotnetInteractive.OpenDocument>{ relativeFilePath: "./Program.cs" } });

        let commandFailed = eventEnvelopes.find(e => e.eventType === dotnetInteractive.CommandFailedType);
        expect(commandFailed).not.to.be.undefined;
        expect((<dotnetInteractive.CommandFailed>(commandFailed.event)).message).to.equal("Project must be opened, send the command 'OpenProject' first.");
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

        let eventEnvelopes: dotnetInteractive.KernelEventEnvelope[] = [];
        kernel.subscribeToKernelEvents(e => {
            eventEnvelopes.push(e);
        });
        await kernel.send({ commandType: dotnetInteractive.RequestDiagnosticsType, command: <dotnetInteractive.RequestDiagnostics>{ code: "Console.WriteLine(1);" } });

        let commandFailed = eventEnvelopes.find(e => e.eventType === dotnetInteractive.CommandFailedType);
        expect(commandFailed).not.to.be.undefined;
        expect((<dotnetInteractive.CommandFailed>(commandFailed.event)).message).to.equal("Document must be opened, send the command 'OpenDocument' first.");
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

        let eventEnvelopes: dotnetInteractive.KernelEventEnvelope[] = [];

        kernel.subscribeToKernelEvents(e => {
            eventEnvelopes.push(e);
        });

        await kernel.send({ commandType: dotnetInteractive.RequestCompletionsType, command: <dotnetInteractive.RequestCompletions>{ code: "Console.WriteLine(1);", linePosition: { character: 1, line: 1 } } });

        let commandFailed = eventEnvelopes.find(e => e.eventType === dotnetInteractive.CommandFailedType);
        expect(commandFailed).not.to.be.undefined;
        expect((<dotnetInteractive.CommandFailed>(commandFailed.event)).message).to.equal("Document must be opened, send the command 'OpenDocument' first.");
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

        let eventEnvelopes: dotnetInteractive.KernelEventEnvelope[] = [];
        kernel.subscribeToKernelEvents(e => {
            eventEnvelopes.push(e);
        });
        await kernel.send({ commandType: dotnetInteractive.RequestSignatureHelpType, command: <dotnetInteractive.RequestSignatureHelp>{ code: "Console.WriteLine(1);", linePosition: { character: 1, line: 1 } } });

        let commandFailed = eventEnvelopes.find(e => e.eventType === dotnetInteractive.CommandFailedType);
        expect(commandFailed).not.to.be.undefined;
        expect((<dotnetInteractive.CommandFailed>(commandFailed.event)).message).to.equal("Document must be opened, send the command 'OpenDocument' first.");
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

        let eventEnvelopes: dotnetInteractive.KernelEventEnvelope[] = [];
        kernel.subscribeToKernelEvents(e => {
            eventEnvelopes.push(e);
        });
        await kernel.send({ commandType: dotnetInteractive.RequestHoverTextType, command: <dotnetInteractive.RequestHoverText>{ code: "Console.WriteLine(1);", linePosition: { character: 1, line: 1 } } });

        let commandFailed = eventEnvelopes.find(e => e.eventType === dotnetInteractive.CommandFailedType);
        expect(commandFailed).not.to.be.undefined;
        expect((<dotnetInteractive.CommandFailed>(commandFailed.event)).message).to.equal("Document must be opened, send the command 'OpenDocument' first.");
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

        let eventEnvelopes: dotnetInteractive.KernelEventEnvelope[] = [];
        kernel.subscribeToKernelEvents(e => {
            eventEnvelopes.push(e);
        });
        await kernel.send({ commandType: dotnetInteractive.SubmitCodeType, command: <dotnetInteractive.SubmitCode>{ code: "Console.WriteLine(1);" } });

        let commandFailed = eventEnvelopes.find(e => e.eventType === dotnetInteractive.CommandFailedType);
        expect(commandFailed).not.to.be.undefined;
        expect((<dotnetInteractive.CommandFailed>(commandFailed.event)).message).to.equal("Document must be opened, send the command 'OpenDocument' first.");
    });

    it("when opening a project it produces the project manifest", async () => {
        let service = createApiServiceSimulator("./simulatorConfigurations/apiService/open_project.json");
        let wasmRunner = createWasmRunnerSimulator();
        let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);

        let eventEnvelopes: dotnetInteractive.KernelEventEnvelope[] = [];
        kernel.subscribeToKernelEvents(e => {
            eventEnvelopes.push(e);
        });
        await kernel.send({
            commandType: dotnetInteractive.OpenProjectType, command: <dotnetInteractive.OpenProject>{
                project: {
                    files: [{
                        relativeFilePath: "./Program.cs",
                        content: "\npublic class Program\n{\n    public static void Main(string[] args)\n    {\n        #region REGION_1\n        var a = 123;\n        #endregion\n\n        #region REGION_2\n        var b = 123;\n        #endregion\n    }\n}"
                    }]
                }
            }
        });

        let projectOpened = eventEnvelopes.find(e => e.eventType === dotnetInteractive.ProjectOpenedType)?.event as dotnetInteractive.ProjectOpened;
        expect(projectOpened).not.to.be.undefined;
        expect(projectOpened.projectItems).to.not.be.empty;
        expect(projectOpened.projectItems).to.deep.equal([{
            relativeFilePath: "./Program.cs", regionNames: ["REGION_1", "REGION_2"]
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

        let eventEnvelopes: dotnetInteractive.KernelEventEnvelope[] = [];
        kernel.subscribeToKernelEvents(e => {
            eventEnvelopes.push(e);
        });
        await kernel.send({ commandType: dotnetInteractive.OpenDocumentType, command: <dotnetInteractive.OpenDocument>{ relativeFilePath: "./Program.cs", regionName: "REGION_2" } });

        let documentOpen = eventEnvelopes.find(e => e.eventType === dotnetInteractive.DocumentOpenedType)?.event as dotnetInteractive.DocumentOpened;
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

        let eventEnvelopes: dotnetInteractive.KernelEventEnvelope[] = [];
        kernel.subscribeToKernelEvents(e => {
            eventEnvelopes.push(e);
        });

        await kernel.send({ commandType: dotnetInteractive.RequestDiagnosticsType, command: <dotnetInteractive.RequestDiagnostics>{ code: "someInt = \"NaN\";" } });

        let diagnostics = eventEnvelopes.find(e => e.eventType === dotnetInteractive.DiagnosticsProducedType)?.event as dotnetInteractive.DiagnosticsProduced;
        expect(diagnostics).not.to.be.undefined;
        expect(diagnostics.diagnostics.length).to.equal(1);
        expect(diagnostics.diagnostics[0]).to.deep.equal({
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

        let eventEnvelopes: dotnetInteractive.KernelEventEnvelope[] = [];
        kernel.subscribeToKernelEvents(e => {
            eventEnvelopes.push(e);
        });

        await kernel.send({ commandType: dotnetInteractive.SubmitCodeType, command: <dotnetInteractive.SubmitCode>{ code: "System.Console.WriteLine(2);" } });

        let standardOutputValueProduced = eventEnvelopes.find(e => e.eventType === dotnetInteractive.StandardOutputValueProducedType)?.event as dotnetInteractive.StandardOutputValueProduced;
        expect(standardOutputValueProduced).not.to.be.undefined;
        expect(standardOutputValueProduced.formattedValues[0].value).to.equal("2");

    });
});

export function openProject(kernel: dotnetInteractive.Kernel, project: dotnetInteractive.Project): Promise<void> {
    return kernel.send({
        commandType: dotnetInteractive.OpenProjectType,
        command: <dotnetInteractive.OpenProject>{
            project: project
        }
    });
}

export async function openProjectAndDocument(kernel: dotnetInteractive.Kernel, project: dotnetInteractive.Project, relativeFilePath: string, regionName?: string): Promise<void> {
    await openProject(kernel, project);

    await kernel.send({
        commandType: dotnetInteractive.OpenDocumentType,
        command: <dotnetInteractive.OpenDocument>{
            relativeFilePath: relativeFilePath,
            regionName: regionName
        }
    });
}