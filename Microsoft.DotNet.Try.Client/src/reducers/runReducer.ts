// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as types from "../constants/ActionTypes";
import { Action } from "../constants/ActionTypes";
import { IRunState } from "../IState";

const initialState: IRunState = {
    exception: undefined,
    output: undefined,
    succeeded: undefined,
    diagnostics: undefined,
    instrumentation: undefined,
    variableLocations: undefined,
    currentInstrumentationStep: undefined
};

export default function configReducer(state: IRunState = initialState, action: Action): IRunState {
    switch (action.type) {
        case types.RUN_CODE_SUCCESS:
        case types.RUN_CODE_RESULT_SPECIFIED:
            return {
                ...state,
                exception: action.exception,
                fullOutput: action.output,
                output: action.instrumentation ? [] : action.output,
                succeeded: action.succeeded,
                diagnostics: action.diagnostics,
                instrumentation: action.instrumentation,
                variableLocations: action.variableLocations,
                currentInstrumentationStep: 0
            };
        case types.RUN_CODE_REQUEST:
            return {
                ...state,
                ...initialState,
            };
        case types.RUN_CODE_FAILURE:
            return {
                ...state,
                exception: action.ex
            };
        case types.NEXT_INSTRUMENT_STEP:
            return {
                ...state,
                currentInstrumentationStep: state.currentInstrumentationStep + 1
            };
        case types.PREV_INSTRUMENT_STEP:
            return {
                ...state,
                currentInstrumentationStep: state.currentInstrumentationStep - 1
            };
        case types.OUTPUT_UPDATED:
            return {
                ...state,
                output: action.output
            };
        case types.UPDATE_WORKSPACE_BUFFER:
            return {
                ...state,
                instrumentation: undefined,
                currentInstrumentationStep: 0
            };
        case types.SET_DIAGNOSTICS: {
            return {
                ...state,
                diagnostics: action.diagnostics,
            };
        }
        case types.COMPILE_CODE_FAILURE: {
            return {
                ...state,
                fullOutput: action.diagnostics.map(v => v.message)
            }
        }
        default:
            return state;
    }
}
