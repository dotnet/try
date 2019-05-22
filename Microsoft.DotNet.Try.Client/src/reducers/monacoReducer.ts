// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as types from "../constants/ActionTypes";
import { Action } from "../constants/ActionTypes";

import { IMonacoState } from "../IState";

const initialState: IMonacoState = {
    editor: undefined,
    editorOptions: { selectOnLineNumbers: true },
    displayedCode: undefined,
    bufferId: "Program.cs"
};

export default function monacoReducer(state: IMonacoState = initialState, action: Action): IMonacoState {
    if (!action) {
        return state;
    }

    switch (action.type) {
        case types.SET_ACTIVE_BUFFER:
            return {
                ...state,
                bufferId: action.bufferId
            };
        case types.LOAD_CODE_SUCCESS:
            return {
                ...state,
                displayedCode: action.sourceCode
            };
        case types.UPDATE_WORKSPACE_BUFFER:
            if (action.bufferId === state.bufferId) {
                return {
                    ...state,
                    displayedCode: action.content
                };
            }
            else {
                return state;
            }
        case types.CONFIGURE_MONACO_EDITOR:
            return {
                ...state,
                editorOptions: action.editorOptions,
                theme: action.theme
            };
        case types.NOTIFY_MONACO_READY:
            return {
                ...state,
                editor: action.editor
            };
        case types.DEFINE_MONACO_EDITOR_THEMES: {
            return {
                ...state,
                themes: action.themes
            };
        }
        default:
            return state;
    }
}
