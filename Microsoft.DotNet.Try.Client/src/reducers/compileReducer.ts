// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as types from "../constants/ActionTypes";
import { Action } from "../constants/ActionTypes";
import { ICompileState } from "../IState";

const initialState: ICompileState =
{
    base64assembly: undefined,
    diagnostics: undefined,
    succeeded: undefined,
    workspaceVersion: undefined
}

export default function compileReducer(state: ICompileState = initialState, action: Action): ICompileState {
    switch (action.type) {
        case types.COMPILE_CODE_FAILURE:
            return {
                ...state,
                succeeded: false,
                base64assembly: undefined,
                diagnostics: action.diagnostics,
                workspaceVersion: action.workspaceVersion
            };
        case types.COMPILE_CODE_REQUEST:
            return {
                ...state,
                succeeded: undefined,
                base64assembly: undefined,
                diagnostics: undefined,
                workspaceVersion: action.workspaceVersion
            };
        case types.COMPILE_CODE_SUCCESS:
            return {
                ...state,
                succeeded: true,
                diagnostics: undefined,
                base64assembly: action.base64assembly,
                workspaceVersion: action.workspaceVersion
            };
        case types.SET_DIAGNOSTICS: {
            return {
                ...state,
                diagnostics: action.diagnostics,
            };
        }
        default:
            return state;
    }
}
