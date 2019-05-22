// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as monacoEditor from "monaco-editor";
import * as types from "../../src/constants/ActionTypes";

import actions from "../../src/actionCreators/actions";
import { expect } from "chai";
import getStore from "../observableAppStore";

describe("CONTROL Action Creators", () => {
    it("should create an action to specify run code result", () => {

        const expectedAction = {
            type: types.RUN_CODE_RESULT_SPECIFIED,
            output: ["some", "output"],
            succeeded: false
        };

        actions.runCodeResultSpecified(["some", "output"], false)
            .should.deep.equal(expectedAction);
    });

    it("should create an action to configure the Monaco Editor", () => {
        var editorOptions: monacoEditor.editor.IEditorOptions = {
            codeLens: false
        };

        var theme = "some string";

        const expectedAction = {
            type: types.CONFIGURE_MONACO_EDITOR,
            editorOptions,
            theme
        };

        actions.configureMonacoEditor(editorOptions, theme)
            .should.deep.equal(expectedAction);
    });

    it("should create an action to show the Monaco Editor", () => {
        const expectedAction = {
            type: types.SHOW_EDITOR
        };

        actions.showEditor()
            .should.deep.equal(expectedAction);
    });

    it("when Monaco Editor is ready then it invokes focus on the editor", () => {
        var store = getStore();
        var focusInvoked = false;

        store.configure([actions.notifyMonacoReady({ focus: () => focusInvoked = true, layout: () => {} })]);

        store.dispatch(actions.focusMonacoEditor());

        expect(focusInvoked).to.be.true;
    });

    it("when Monaco Editor is not ready then it does not fail", () => {
        var store = getStore();

        store.dispatch(actions.focusMonacoEditor());
    });
});
