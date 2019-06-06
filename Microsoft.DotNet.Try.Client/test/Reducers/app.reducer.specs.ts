// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IConfigState, IMonacoState, IUiState, IWorkspaceInfo } from "../../src/IState";
import { emptyWorkspace } from "../testResources";
import { defaultWorkspace } from "../testResources";
import reducer from "../../src/reducers/app.reducer";
import { should } from "chai";

should();

describe("app.reducer", () => {
    it("should contain initial state for config", () => {
        var expectedConfig: IConfigState = {
            client: undefined,
            completionProvider: "roslyn",
            from: "default",
            hostOrigin: undefined,
            defaultWorkspace: { ...defaultWorkspace },
            defaultCodeFragment: defaultWorkspace.buffers[0].content,
            version: 1,
            applicationInsightsClient: undefined
        };

        reducer(undefined, { type: undefined }).config.should.deep.equal(expectedConfig);
    });

    it("should contain initial state for monaco", () => {
        var expectedConfig: IMonacoState = {
            editor: undefined,
            editorOptions: {
                selectOnLineNumbers: true
            },
            displayedCode: undefined,
            bufferId: "Program.cs",
            language: "csharp"
        };

        reducer(undefined, { type: undefined }).monaco.should.deep.equal(expectedConfig);
    });

    it("should contain initial state for run", () => {
        var expectedConfig: IConfigState = {
            exception: undefined,
            output: undefined,
            succeeded: undefined,
            diagnostics: undefined,
            instrumentation: undefined,
            variableLocations: undefined,
            currentInstrumentationStep: undefined
        };

        reducer(undefined, { type: undefined }).run.should.deep.equal(expectedConfig);
    });

    it("should contain initial state for ui", () => {
        var expectedConfig: IUiState = {
            canShowGitHubPanel: false,
            canEdit: false,
            canRun: false,
            showEditor: true,
            isRunning: false,
            enableBranding: true
        };

        reducer(undefined, { type: undefined }).ui.should.deep.equal(expectedConfig);
    });

    it("should contain initial state for workpaceInfo", () => {
        var expectedConfig: IWorkspaceInfo = {
            originType: "undefinedOrigin"
        };

        reducer(undefined, { type: undefined }).workspaceInfo.should.deep.equal(expectedConfig);
    });

    it("should contain initial state for workpace", () => {
        var expectedConfig = emptyWorkspace;

        reducer(undefined, { type: undefined }).workspace.workspace.should.deep.equal(expectedConfig);
    });
});
