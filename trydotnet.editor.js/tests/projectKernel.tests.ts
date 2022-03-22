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
        await kernel.send({ commandType: dotnetInteractive.OpenDocumentType, command: <dotnetInteractive.OpenDocument>{ relativeFilePath: "Program.cs" } });

        let commandFailed = eventEnvelopes.find(e => e.eventType === dotnetInteractive.CommandFailedType);
        expect(commandFailed).not.to.be.undefined;
        expect((<dotnetInteractive.CommandFailed>(commandFailed.event)).message).to.equal("Project is not loaded");
    });

    it("cannot request diagnostics if there is no open document", async () => {
        let service = createApiServiceSimulator("./simulatorConfigurations/apiService/cannot_request_diagnostics_if_there_is_no_open_document.json");
        let wasmRunner = createWasmRunnerSimulator();
        let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);

        await openProject(
            kernel,
            {
                files: [{
                    relativeFilePath: "program.cs",
                    content: ""
                }]
            });

        let eventEnvelopes: dotnetInteractive.KernelEventEnvelope[] = [];
        kernel.subscribeToKernelEvents(e => {
            eventEnvelopes.push(e);
        });
        await kernel.send({ commandType: dotnetInteractive.RequestDiagnosticsType, command: <dotnetInteractive.RequestDiagnostics>{ code: "Console.WriteLine(1);" } });

        let commandFailed = eventEnvelopes.find(e => e.eventType === dotnetInteractive.CommandFailedType);
        expect(commandFailed).not.to.be.undefined;
        expect((<dotnetInteractive.CommandFailed>(commandFailed.event)).message).to.equal("No Open document found");
    });

    it("cannot request completions if there is no open document", async () => {
        let service = createApiServiceSimulator("./simulatorConfigurations/apiService/cannot_request_completions_if_there_is_no_open_document.json");
        let wasmRunner = createWasmRunnerSimulator();
        let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);

        await openProject(
            kernel,
            {
                files: [{
                    relativeFilePath: "program.cs",
                    content: ""
                }]
            });

        let eventEnvelopes: dotnetInteractive.KernelEventEnvelope[] = [];

        kernel.subscribeToKernelEvents(e => {
            eventEnvelopes.push(e);
        });

        await kernel.send({ commandType: dotnetInteractive.RequestCompletionsType, command: <dotnetInteractive.RequestCompletions>{ code: "Console.WriteLine(1);", linePosition: { character: 1, line: 1 } } });

        let commandFailed = eventEnvelopes.find(e => e.eventType === dotnetInteractive.CommandFailedType);
        expect(commandFailed).not.to.be.undefined;
        expect((<dotnetInteractive.CommandFailed>(commandFailed.event)).message).to.equal("No Open document found");
    });

    it("cannot request signaturehelp if there is no open document", async () => {
        let service = createApiServiceSimulator("./simulatorConfigurations/apiService/cannot_request_signaturehelp_if_there_is_no_open_document.json");
        let wasmRunner = createWasmRunnerSimulator();
        let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);

        await openProject(
            kernel,
            {
                files: [{
                    relativeFilePath: "program.cs",
                    content: ""
                }]
            });

        let eventEnvelopes: dotnetInteractive.KernelEventEnvelope[] = [];
        kernel.subscribeToKernelEvents(e => {
            eventEnvelopes.push(e);
        });
        await kernel.send({ commandType: dotnetInteractive.RequestSignatureHelpType, command: <dotnetInteractive.RequestSignatureHelp>{ code: "Console.WriteLine(1);", linePosition: { character: 1, line: 1 } } });

        let commandFailed = eventEnvelopes.find(e => e.eventType === dotnetInteractive.CommandFailedType);
        expect(commandFailed).not.to.be.undefined;
        expect((<dotnetInteractive.CommandFailed>(commandFailed.event)).message).to.equal("No Open document found");
    });

    it("cannot request hovertext if there is no open document", async () => {
        let service = createApiServiceSimulator("./simulatorConfigurations/apiService/cannot_request_hovertext_if_there_is_no_open_document.json");
        let wasmRunner = createWasmRunnerSimulator();
        let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);

        await openProject(
            kernel,
            {
                files: [{
                    relativeFilePath: "program.cs",
                    content: ""
                }]
            });

        let eventEnvelopes: dotnetInteractive.KernelEventEnvelope[] = [];
        kernel.subscribeToKernelEvents(e => {
            eventEnvelopes.push(e);
        });
        await kernel.send({ commandType: dotnetInteractive.RequestHoverTextType, command: <dotnetInteractive.RequestHoverText>{ code: "Console.WriteLine(1);", linePosition: { character: 1, line: 1 } } });

        let commandFailed = eventEnvelopes.find(e => e.eventType === dotnetInteractive.CommandFailedType);
        expect(commandFailed).not.to.be.undefined;
        expect((<dotnetInteractive.CommandFailed>(commandFailed.event)).message).to.equal("No Open document found");
    });

    it("cannot submitCode if there is no open document", async () => {
        let service = createApiServiceSimulator("./simulatorConfigurations/apiService/cannot_submitCode_if_there_is_no_open_document.json");
        let wasmRunner = createWasmRunnerSimulator();
        let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);

        await openProject(
            kernel,
            {
                files: [{
                    relativeFilePath: "program.cs",
                    content: ""
                }]
            });

        let eventEnvelopes: dotnetInteractive.KernelEventEnvelope[] = [];
        kernel.subscribeToKernelEvents(e => {
            eventEnvelopes.push(e);
        });
        await kernel.send({ commandType: dotnetInteractive.SubmitCodeType, command: <dotnetInteractive.SubmitCode>{ code: "Console.WriteLine(1);" } });

        let commandFailed = eventEnvelopes.find(e => e.eventType === dotnetInteractive.CommandFailedType);
        expect(commandFailed).not.to.be.undefined;
        expect((<dotnetInteractive.CommandFailed>(commandFailed.event)).message).to.equal("No Open document found");
    });

    it("when opening a project it produces the project manifest", async () => {
        let service = createApiServiceSimulator("./simulatorConfigurations/apiService/when_opening_a_project_it_produces_the_project_manifest.json");
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
                        relativeFilePath: "program.cs",
                        content: "Console.WriteLine(1);"
                    }]
                }
            }
        });

        let projectOpened = eventEnvelopes.find(e => e.eventType === dotnetInteractive.ProjectOpenedType)?.event as dotnetInteractive.ProjectOpened;
        expect(projectOpened).not.to.be.undefined;
        expect(projectOpened.projectItems).to.not.be.empty;
        expect(projectOpened.projectItems).to.deep.equal([{ relativeFilePath: "program.cs", regionNames: [] }]);
    });

    it("when opening a document it produces documentOpen event with content", async () => {
        let service = createApiServiceSimulator("./simulatorConfigurations/apiService/when_opening_a_document_it_produces_documentOpen_event_with_content.json");
        let wasmRunner = createWasmRunnerSimulator();
        let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);

        await openProject(
            kernel,
            {
                files: [{
                    relativeFilePath: "program.cs",
                    content: "Console.WriteLine(1);"
                }]
            });

        let eventEnvelopes: dotnetInteractive.KernelEventEnvelope[] = [];
        kernel.subscribeToKernelEvents(e => {
            eventEnvelopes.push(e);
        });
        await kernel.send({ commandType: dotnetInteractive.OpenDocumentType, command: <dotnetInteractive.OpenDocument>{ relativeFilePath: "program.cs" } });

        let documentOpen = eventEnvelopes.find(e => e.eventType === dotnetInteractive.DocumentOpenedType)?.event as dotnetInteractive.DocumentOpened;
        expect(documentOpen).not.to.be.undefined;
        expect(documentOpen.relativeFilePath).to.equal("program.cs");
        expect(documentOpen.content).to.equal("Console.WriteLine(1);");
    });

    it("produces diagnostics for the open document", async () => {
        let service = createApiServiceSimulator("./simulatorConfigurations/apiService/produces_diagnostics_for_the_open_document.json");
        let wasmRunner = createWasmRunnerSimulator();
        let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);

        await openProjectAndDocument(
            kernel,
            {
                files: [{
                    relativeFilePath: "program.cs",
                    content: "Console.WriteLine(1);"
                }, {
                    relativeFilePath: "class.cs",
                    content: "public class A {}"
                }]
            },
            "program.cs");

        let eventEnvelopes: dotnetInteractive.KernelEventEnvelope[] = [];
        kernel.subscribeToKernelEvents(e => {
            eventEnvelopes.push(e);
        });
        await kernel.send({ commandType: dotnetInteractive.RequestDiagnosticsType, command: <dotnetInteractive.RequestDiagnostics>{ code: "Conzole" } });

        let diagnostics = eventEnvelopes.find(e => e.eventType === dotnetInteractive.DiagnosticsProducedType)?.event as dotnetInteractive.DiagnosticsProduced;
        expect(diagnostics).not.to.be.undefined;
        expect(diagnostics.diagnostics.length).to.equal(1);
        expect(diagnostics.diagnostics[0]).to.deep.equal({
            linePositionSpan: {
                start: { line: 1, character: 1 },
                end: { line: 1, character: 7 }
            },
            severity: dotnetInteractive.DiagnosticSeverity.Error,
            code: "Conzole",
            message: "Error here!"
        });
    });

    it("executes correct code", async () => {
        // fix this to be handling console writeline
        let service = createApiServiceSimulator("./simulatorConfigurations/apiService/executes_correct_code.json");
        let wasmRunner = createWasmRunnerSimulator("./simulatorConfigurations/wasmRunner/executes_correct_code.json");
        let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);

        await openProjectAndDocument(
            kernel,
            {
                files: [{
                    relativeFilePath: "program.cs",
                    content: "Console.WriteLine(1);"
                }, {
                    relativeFilePath: "class.cs",
                    content: "public class A {}"
                }]
            },
            "program.cs");

        let eventEnvelopes: dotnetInteractive.KernelEventEnvelope[] = [];
        kernel.subscribeToKernelEvents(e => {
            eventEnvelopes.push(e);
        });

        await kernel.send({ commandType: dotnetInteractive.SubmitCodeType, command: <dotnetInteractive.SubmitCode>{ code: "Console.WriteLine(2);" } });

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