// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { describe } from "mocha";

import * as tryDotNetEditor from "../src/tryDotNetEditor";
import * as monacoEditorSimulator from "./monacoEditorSimulator";
import * as nullMessageBus from "./nullMessageBus";
import * as dotnetInteractive from "@microsoft/dotnet-interactive";
import * as CSharpProjectKernelWithWASMRunner from "../src/ProjectKernelWithWASMRunner";
import { createApiServiceSimulator } from "./apiServiceSimulator";
import { createWasmRunnerSimulator } from "./wasmRunnerSimulator";
import { delay } from "./testHelpers";

describe("when loading workspace", () => {
    before(() => {
        dotnetInteractive.Logger.configure("debug", (_entry) => {
            //  console.log(entry.message);
        });
    });

    it("configures the editor languge", async () => {
        let mainWindowMessageBus = new nullMessageBus.NullMessageBus();
        let service = createApiServiceSimulator();
        let wasmRunner = createWasmRunnerSimulator();
        let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);
        let tdn = new tryDotNetEditor.TryDotNetEditor(mainWindowMessageBus, kernel);
        tdn.editor = new monacoEditorSimulator.MonacoEditorSimulator();

        let project = <dotnetInteractive.Project>{
            files: []
        };
        await tdn.openProject(project);

        expect(tdn.editor.getLanguage()).to.equal("csharp");

    });

    it("configures the editor code", async () => {
        let mainWindowMessageBus = new nullMessageBus.NullMessageBus();
        let service = createApiServiceSimulator("./simulatorConfigurations/apiService/configures_the_editor_code.json");
        let wasmRunner = createWasmRunnerSimulator();
        let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);
        let tdn = new tryDotNetEditor.TryDotNetEditor(mainWindowMessageBus, kernel);
        tdn.editor = new monacoEditorSimulator.MonacoEditorSimulator();

        let project = <dotnetInteractive.Project>{
            files: [{ relativePath: "Program.cs", content: "public class C { }" }]
        };
        await tdn.openProject(project);
        await tdn.openDocument({ path: "Program.cs" });

        expect(tdn.editor.getCode()).to.equal("public class C { }");

    });
});

describe("when user types in editor", () => {
    it("the editor content and positons are updated", async () => {
        let mainWindowMessageBus = new nullMessageBus.NullMessageBus();
        let service = createApiServiceSimulator();
        let wasmRunner = createWasmRunnerSimulator();
        let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);
        let tdn = new tryDotNetEditor.TryDotNetEditor(mainWindowMessageBus, kernel);
        let editor = new monacoEditorSimulator.MonacoEditorSimulator();
        tdn.editor = editor;

        let project = <dotnetInteractive.Project>{
            files: [{ relativePath: "Program.cs", content: "" }]
        };

        await tdn.openProject(project);
        await tdn.openDocument({ path: "Program.cs" });

        const userContent = "public class C { }";

        editor.type(userContent);

        expect(tdn.editor.getCode()).to.equal(userContent);

        expect(tdn.editor.getPosition()).to.deep.equal({ line: 1, column: userContent.length + 1 });
    });

    it("the editor asks the kernel for diagnostics", async () => {
        dotnetInteractive.Logger.configure("debug", (_entry) => {
            //  console.log(entry.message);
        });

        let mainWindowMessageBus = new nullMessageBus.NullMessageBus();
        let service = createApiServiceSimulator("./simulatorConfigurations/apiService/the_editor_asks_the_kernel_for_diagnostics.json");
        let wasmRunner = createWasmRunnerSimulator();
        let kernel = new CSharpProjectKernelWithWASMRunner.ProjectKernelWithWASMRunner('csharpProject', wasmRunner, service);

        let tdn = new tryDotNetEditor.TryDotNetEditor(mainWindowMessageBus, kernel);
        let editor = new monacoEditorSimulator.MonacoEditorSimulator();
        tdn.editor = editor;

        let project = <dotnetInteractive.Project>{
            files: [{ relativePath: "Program.cs", content: "" }]
        };

        await tdn.openProject(project);
        await tdn.openDocument({ path: "Program.cs" });

        const userContent = "public class C { }";

        editor.type(userContent);

        await delay(1000);

        expect(editor.diagnostics).not.to.be.empty;
        expect(editor.diagnostics[0]).to.deep.equal({
            "message": "Error here!",
            "code": "public class C { }",
            "severity": "error",
            "linePositionSpan": {
                "start": {
                    "line": 1,
                    "character": 1
                },
                "end": {
                    "line": 1,
                    "character": 19
                }
            }
        });

    });
});

