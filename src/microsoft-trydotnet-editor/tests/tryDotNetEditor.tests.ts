// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { describe } from "mocha";

import * as tryDotNetEditor from "../src/tryDotNetEditor";
import * as monacoEditorSimulator from "./monacoEditorSimulator";
import * as rxjs from 'rxjs';
import * as polyglotNotebooks from "@microsoft/polyglot-notebooks";
import * as CSharpProjectKernelWithWASMRunner from "../src/ProjectKernelWithWASMRunner";
import { createApiServiceSimulator } from "./apiServiceSimulator";
import { createWasmRunnerSimulator } from "./wasmRunnerSimulator";
import { delay } from "./testHelpers";

describe("trydotnet", () => {
    describe("when loading workspace", () => {
        before(() => {
            polyglotNotebooks.Logger.configure("debug", (_entry) => {
                //     console.log(_entry.message);
            });
        });

        it("configures the editor languge", async () => {

            let service = createApiServiceSimulator("./simulatorConfigurations/apiService/open_document.json");
            let wasmRunner = createWasmRunnerSimulator();
            let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);
            let tdn = new tryDotNetEditor.TryDotNetEditor((_) => { }, new rxjs.Subject<any>(), kernel);
            tdn.editor = new monacoEditorSimulator.MonacoEditorSimulator();

            let project = <polyglotNotebooks.Project>{
                files: [{
                    relativeFilePath: "./Program.cs",
                    content: "\npublic class Program\n{\n    public static void Main(string[] args)\n    {\n        #region REGION_1\n        var a = 123;\n        #endregion\n\n        #region REGION_2\n        var b = 123;\n        #endregion\n    }\n}"
                }]
            };

            await tdn.openProject(project);

            expect(tdn.editor.getLanguage()).to.equal("csharp");

        });

        it("configures the editor code", async () => {
            let service = createApiServiceSimulator("./simulatorConfigurations/apiService/open_document.json");
            let wasmRunner = createWasmRunnerSimulator();
            let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);
            let tdn = new tryDotNetEditor.TryDotNetEditor((_) => { }, new rxjs.Subject<any>(), kernel);
            tdn.editor = new monacoEditorSimulator.MonacoEditorSimulator();

            let project = <polyglotNotebooks.Project>{
                files: [{
                    relativeFilePath: "./Program.cs",
                    content: "\npublic class Program\n{\n    public static void Main(string[] args)\n    {\n        #region REGION_1\n        var a = 123;\n        #endregion\n\n        #region REGION_2\n        var b = 123;\n        #endregion\n    }\n}"
                }]
            };
            await tdn.openProject(project);
            await tdn.openDocument({ relativeFilePath: "./Program.cs" });

            expect(tdn.editor.getCode()).to.equal("\npublic class Program\n{\n    public static void Main(string[] args)\n    {\n        #region REGION_1\n        var a = 123;\n        #endregion\n\n        #region REGION_2\n        var b = 123;\n        #endregion\n    }\n}");

        });

        it("configures the editor code when laoding a different project", async () => {
            let service = createApiServiceSimulator("./simulatorConfigurations/apiService/update_project.json");
            let wasmRunner = createWasmRunnerSimulator();
            let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);
            let tdn = new tryDotNetEditor.TryDotNetEditor((_) => { }, new rxjs.Subject<any>(), kernel);
            tdn.editor = new monacoEditorSimulator.MonacoEditorSimulator();

            let Originalproject = <polyglotNotebooks.Project>{
                files: [{
                    relativeFilePath: "./program.cs",
                    content: "\nusing System;\nusing System.Collections.Generic;\nusing System.Linq;\nusing System.Text;\nusing System.Globalization;\nusing System.Text.RegularExpressions;\nnamespace Program {\n    class Program {\n        static void Main(string[] args){\n            #region controller\n            #endregion\n        }\n    }\n}"
                }]
            };
            await tdn.openProject(Originalproject);
            await tdn.openDocument({ relativeFilePath: "./program.cs", regionName: "controller" });

            expect(tdn.editor.getCode()).to.equal("");

            let newProject = <polyglotNotebooks.Project>{
                files: [{
                    relativeFilePath: "./program.cs",
                    content: "\nusing System;\nusing System.Collections.Generic;\nusing System.Linq;\nusing System.Text;\nusing System.Globalization;\nusing System.Text.RegularExpressions;\nnamespace Program {\n    class Program {\n        static void Main(string[] args){\n            #region controller\n            Console.WriteLine(123);\n            #endregion\n        }\n    }\n}"
                }]
            };
            await tdn.openProject(newProject);
            await tdn.openDocument({ relativeFilePath: "./program.cs", regionName: "controller" });

            expect(tdn.editor.getCode()).to.equal("Console.WriteLine(123);");

        });
    });

    describe("when user types in editor", () => {
        it("the editor content and positons are updated", async () => {
            let service = createApiServiceSimulator("./simulatorConfigurations/apiService/open_document.json");
            let wasmRunner = createWasmRunnerSimulator();
            let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);
            let tdn = new tryDotNetEditor.TryDotNetEditor((_) => { }, new rxjs.Subject<any>(), kernel);
            let editor = new monacoEditorSimulator.MonacoEditorSimulator();
            tdn.editor = editor;

            let project = <polyglotNotebooks.Project>{
                files: [{
                    relativeFilePath: "./Program.cs",
                    content: "\npublic class Program\n{\n    public static void Main(string[] args)\n    {\n        #region REGION_1\n        var a = 123;\n        #endregion\n\n        #region REGION_2\n        var b = 123;\n        #endregion\n    }\n}"
                }]
            };

            await tdn.openProject(project);
            await tdn.openDocument({ relativeFilePath: "./Program.cs" });

            const userContent = "public class C { }\n";

            editor.type(userContent);

            expect(tdn.editor.getCode()).to.contain(userContent);

            expect(tdn.editor.getPosition()).to.deep.equal({ line: 2, column: 1 });
        });

        it("the editor asks the kernel for diagnostics", async () => {

            let service = createApiServiceSimulator("./simulatorConfigurations/apiService/diagnostics_produced_with_errors_in_code.json");
            let wasmRunner = createWasmRunnerSimulator();
            let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);

            let tdn = new tryDotNetEditor.TryDotNetEditor((_) => { }, new rxjs.Subject<any>(), kernel);
            let editor = new monacoEditorSimulator.MonacoEditorSimulator();
            tdn.editor = editor;

            let project = <polyglotNotebooks.Project>{
                files: [{
                    relativeFilePath: "./Program.cs",
                    content: "\npublic class Program\n{\n    public static void Main(string[] args)\n    {\n        int someInt = 1;\n        #region test-region\n        #endregion\n    }\n}\n"
                }]
            };

            await tdn.openProject(project);
            await tdn.openDocument({ relativeFilePath: "./Program.cs", regionName: "test-region" });

            const userContent = "someInt = \"NaN\";";

            editor.type(userContent);

            await delay(1000);

            expect(editor.diagnostics).not.to.be.empty;
            expect(editor.diagnostics[0]).to.deep.equal({
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

        it("diagnostic events with hidden severity are not set markers on editor", async () => {

            let service = createApiServiceSimulator("./simulatorConfigurations/apiService/diagnostics_produced_with_hidden_severity.json");
            let wasmRunner = createWasmRunnerSimulator();
            let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);

            let tdn = new tryDotNetEditor.TryDotNetEditor((_) => { }, new rxjs.Subject<any>(), kernel);
            let editor = new monacoEditorSimulator.MonacoEditorSimulator();
            tdn.editor = editor;

            let project = <polyglotNotebooks.Project>{
                files: [{
                    relativeFilePath: "./Program.cs",
                    content: "\npublic class Program\n{\n    public static void Main(string[] args)\n    {\n        int someInt = 1;\n        #region test-region\n        #endregion\n    }\n}\n"
                }]
            };

            await tdn.openProject(project);
            await tdn.openDocument({ relativeFilePath: "./Program.cs", regionName: "test-region" });

            const userContent = "someInt = 4;";

            editor.type(userContent);

            await delay(1000);

            const markers = editor.getMarkers(); //?

            expect(markers).to.be.empty;


        });
    });

    it("diagnostic events set markers on editor", async () => {

        let service = createApiServiceSimulator("./simulatorConfigurations/apiService/diagnostics_produced_with_errors_in_code.json");
        let wasmRunner = createWasmRunnerSimulator();
        let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);

        let tdn = new tryDotNetEditor.TryDotNetEditor((_) => { }, new rxjs.Subject<any>(), kernel);
        let editor = new monacoEditorSimulator.MonacoEditorSimulator();
        tdn.editor = editor;

        let project = <polyglotNotebooks.Project>{
            files: [{
                relativeFilePath: "./Program.cs",
                content: "\npublic class Program\n{\n    public static void Main(string[] args)\n    {\n        int someInt = 1;\n        #region test-region\n        #endregion\n    }\n}\n"
            }]
        };

        await tdn.openProject(project);
        await tdn.openDocument({ relativeFilePath: "./Program.cs", regionName: "test-region" });

        const userContent = "someInt = \"NaN\";";

        editor.type(userContent);

        await delay(1000);

        const markers = editor.getMarkers();

        expect(markers).not.to.be.empty;
        expect(markers[0]).to.deep.equal({
            "endColumn": 16,
            "endLineNumber": 1,
            "message": "(1,11): error CS0029: Cannot implicitly convert type 'string' to 'int'",
            "severity": 8,
            "startColumn": 11,
            "startLineNumber": 1,
        });

    });
});
