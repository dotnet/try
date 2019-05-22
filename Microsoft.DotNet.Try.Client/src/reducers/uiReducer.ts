// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as types from "../constants/ActionTypes";
import { Action } from "../constants/ActionTypes";
import { IUiState } from "../IState";

const initialState: IUiState = {
    canShowGitHubPanel: false,
    canEdit: false,
    canRun: false,
    showEditor: true,
    isRunning: false,
    enableBranding: true
};

export default function uiReducer(state = initialState, action: Action): IUiState {
    if (!state) {
        state = initialState;
    }

    switch (action.type) {
        case types.CAN_SHOW_GITHUB_PANEL:
            return {
                ...state,
                canShowGitHubPanel: action.canShow
            };
        case types.LOAD_CODE_SUCCESS:
        case types.LOAD_CODE_FAILURE:
        case types.COMPILE_CODE_FAILURE:
        case types.COMPILE_CODE_SUCCESS:
        case types.RUN_CODE_SUCCESS:
        case types.RUN_CODE_FAILURE:
            return {
                ...state,
                canEdit: true,
                canRun: true,
                isRunning: false
            };
        case types.LOAD_CODE_REQUEST:
            return {
                ...state,
                canEdit: false,
                canRun: false,
                isRunning: false
            };
        case types.RUN_CODE_REQUEST:
            return {
                ...state,
                canEdit: false,
                canRun: false,
                isRunning: true
            };
        case types.HIDE_EDITOR:
            return {
                ...state,
                showEditor: false
            };
        case types.SHOW_EDITOR:
            return {
                ...state,
                showEditor: true
            };
        case types.CONFIGURE_ENABLE_INSTRUMENTATION:
            return {
                ...state,
                instrumentationActive: true
            };
        case types.CONFIGURE_BRANDING:
            return {
                ...state,
                enableBranding: action.visible
            };
        default:
            return state;
    }
}
