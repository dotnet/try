// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as monacoEditor from "monaco-editor";
import * as types from "../constants/ActionTypes";
import { Action } from "../constants/ActionTypes";
import IState from "../IState";
import { ThunkDispatch, ThunkAction } from "redux-thunk";
import { AnyAction, ActionCreator } from "redux";

export function runCodeResultSpecified(output: string[], succeeded: boolean): Action {
    return {
        type: types.RUN_CODE_RESULT_SPECIFIED,
        output: output,
        succeeded: succeeded
    };
}

export function configureMonacoEditor(editorOptions: monacoEditor.editor.IEditorOptions, theme: string): Action {
    return {
        type: types.CONFIGURE_MONACO_EDITOR,
        editorOptions,
        theme
    };
}

export function defineMonacoEditorThemes(themes: { [x: string]: monacoEditor.editor.IStandaloneThemeData }) {
    return {
        type: types.DEFINE_MONACO_EDITOR_THEMES,
        themes
    };
}

export function showEditor(): Action {
    return {
        type: types.SHOW_EDITOR
    };
}

export const focusMonacoEditor: ActionCreator<ThunkAction<AnyAction, IState, void, AnyAction>> = () =>
    (_dispatch: ThunkDispatch<IState, void, AnyAction>, getState: () => IState) => {
        const state = getState();
        const editor = state.monaco.editor;

        if (editor) {
            editor.focus();
        }

        return {
            type: "EDITOR_FOCUSED"
        };
    };
