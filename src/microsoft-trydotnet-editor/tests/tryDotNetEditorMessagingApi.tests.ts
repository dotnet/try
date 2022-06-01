// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { describe } from "mocha";

import * as tryDotNetEditor from "../src/tryDotNetEditor";
import * as monacoEditorSimulator from "./monacoEditorSimulator";
import * as rxjs from 'rxjs';
import * as dotnetInteractive from "@microsoft/dotnet-interactive";
import * as CSharpProjectKernelWithWASMRunner from "../src/ProjectKernelWithWASMRunner";
import { createApiServiceSimulator } from "./apiServiceSimulator";
import { createWasmRunnerSimulator } from "./wasmRunnerSimulator";
import { OpenDocument, OpenProject } from "../src/newContract";

describe("trydotnet", () => {
    describe("host messages are received", () => {
        describe("project loading", () => {
            it("loads project from OpenProject and sends response", async () => {
                let responses: any[] = [];
                let service = createApiServiceSimulator("./simulatorConfigurations/apiService/open_document.json");
                let wasmRunner = createWasmRunnerSimulator();
                let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);
                let tdn = new tryDotNetEditor.TryDotNetEditor((r) => { responses.push(r); }, new rxjs.Subject<any>(), kernel);
                tdn.editor = new monacoEditorSimulator.MonacoEditorSimulator();
                let project = <dotnetInteractive.Project>{
                    files: [{
                        relativeFilePath: "./Program.cs",
                        content: "\npublic class Program\n{\n    public static void Main(string[] args)\n    {\n        #region REGION_1\n        var a = 123;\n        #endregion\n\n        #region REGION_2\n        var b = 123;\n        #endregion\n    }\n}"
                    }]
                };
                await tdn.handleHostMessage(<OpenProject>{
                    type: dotnetInteractive.OpenProjectType,
                    requestId: "1",
                    editorId: "0",
                    project: project
                });

                expect(responses.find(r =>
                    r.type === dotnetInteractive.ProjectOpenedType)).to.eql({
                        editorId: '0',
                        projectItems:
                            [{
                                regionNames: ['REGION_1', 'REGION_2'],
                                regionsContent: { REGION_1: 'var a = 123;', REGION_2: 'var b = 123;' },
                                relativeFilePath: './Program.cs'
                            }],
                        requestId: '1',
                        type: 'ProjectOpened'
                    });

            });

            it("opens documetns from OpenDocument and sends response", async () => {
                let responses: any[] = [];
                let service = createApiServiceSimulator("./simulatorConfigurations/apiService/open_document.json");
                let wasmRunner = createWasmRunnerSimulator();
                let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);
                let tdn = new tryDotNetEditor.TryDotNetEditor((r) => { responses.push(r); }, new rxjs.Subject<any>(), kernel);
                tdn.editor = new monacoEditorSimulator.MonacoEditorSimulator();
                let project = <dotnetInteractive.Project>{
                    files: [{
                        relativeFilePath: "./Program.cs",
                        content: "\npublic class Program\n{\n    public static void Main(string[] args)\n    {\n        #region REGION_1\n        var a = 123;\n        #endregion\n\n        #region REGION_2\n        var b = 123;\n        #endregion\n    }\n}"
                    }]
                };
                await tdn.handleHostMessage(<OpenProject>{
                    type: dotnetInteractive.OpenProjectType,
                    requestId: "1",
                    editorId: "0",
                    project: project
                });

                await tdn.handleHostMessage(<OpenDocument>{
                    type: dotnetInteractive.OpenDocumentType,
                    requestId: "2",
                    editorId: "0",
                    relativeFilePath: "./Program.cs"
                });

                expect(responses.find(r =>
                    r.type === dotnetInteractive.DocumentOpenedType)).to.eql({
                        content: '\npublic class Program\n{\n    public static void Main(string[] args)\n    {\n        #region REGION_1\n        var a = 123;\n        #endregion\n\n        #region REGION_2\n        var b = 123;\n        #endregion\n    }\n}',
                        editorId: '0',
                        relativeFilePath: './Program.cs',
                        requestId: '2',
                        type: 'DocumentOpened'
                    });

            });
        });
    });
});