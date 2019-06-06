// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IMonacoState } from "../../src/IState";
import actions from "../../src/actionCreators/actions";
import reducer from "../../src/reducers/monacoReducer";

describe("Monaco Reducer", () => {
    it("should return the initial state", () => {
        reducer(undefined, { type: undefined }).should.deep.equal({
            editor: undefined,
            editorOptions: { selectOnLineNumbers: true },
            displayedCode: undefined,
            bufferId: "Program.cs",
            language: "csharp"
        });
    });

    it("should handle LOAD_CODE_SUCCESS and update state", () => {
        const originalState: IMonacoState = { salt: 1 };
        const action = actions.loadCodeSuccess("21 * 2");
        const result = {
            salt: 1,
            displayedCode: "21 * 2"
        };

        reducer(originalState, action)
            .should.deep.equal(result).and.not.equal(originalState);
    });

    it("should handle UPDATE_WORKSPACE_BUFFER and update state if are on same buffer id", () => {
        const originalState = { salt: 1, displayedCode: "old source", bufferId: "rightBuffer" };
        const action = actions.updateWorkspaceBuffer("new source", "rightBuffer");
        const result = {
            bufferId: "rightBuffer",
            salt: 1,
            displayedCode: "new source"
        };

        reducer(originalState, action)
            .should.deep.equal(result).and.not.equal(originalState);
    });

    it("should handle UPDATE_WORKSPACE_BUFFER and not update state if are on same buffer id", () => {
        const originalState = { salt: 1, displayedCode: "old source", bufferId: "rightBuffer" };
        const action = actions.updateWorkspaceBuffer("new source", "wrong");
        const result = {
            bufferId: "rightBuffer",
            salt: 1,
            displayedCode: "new source"
        };

        reducer(originalState, action)
            .should.deep.equal(originalState).and.not.equal(result);
    });

    it("should handle SET_CODE and update state", () => {
        const originalState = { salt: 1, bufferId: "Program.cs" };
        const action = actions.setActiveBuffer("bufferId");
        const result = {
            salt: 1,
            bufferId: "bufferId"
        };

        reducer(originalState, action)
            .should.deep.equal(result)
            .and.not.equal(originalState);
    });

    it("should handle CONFIGURE_MONACO_EDITOR and update state", () => {
        const originalState = { salt: 1, editorOptions: {} };
        const action = actions.configureMonacoEditor({ codeLens: false }, "vs-dark");
        const result = {
            salt: 1,
            editorOptions: { codeLens: false },
            theme: "vs-dark"
        };

        reducer(originalState, action)
            .should.deep.equal(result).and.not.equal(originalState);
    });

    it("should handle NOTIFY_MONACO_READY and update state", () => {
        const monaco = { focus: () => { }, layout: () => { } };
        const originalState = { salt: 1 };
        const action = actions.notifyMonacoReady(monaco);
        const result = {
            salt: 1,
            editor: monaco
        };

        reducer(originalState, action)
            .should.deep.equal(result).and.not.equal(originalState);
    });

    it("should handle SET_WORKSPACE_LANGUAGE and update state", () => {
       
        const originalState = { salt: 1 };
        const action = actions.setWorkspaceLanguage("fsharp");
        const result = {
            salt: 1,
            language:"fsharp"
        };

        reducer(originalState, action)
            .should.deep.equal(result).and.not.equal(originalState);
    });
});
