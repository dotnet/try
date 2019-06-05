// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as types from "../constants/ActionTypes";
import { Action } from "../constants/ActionTypes";
import { IWasmRunnerState } from "../IState";



const initialState: IWasmRunnerState = {
    callback: undefined,
    payload: undefined,
    sequence: 0
};

export default function wasmRunnerReducer(state: IWasmRunnerState = initialState, action: Action): IWasmRunnerState {
    switch (action.type) {
        case types.SEND_WASMRUNNER_MESSAGE:
            return {
                callback: action.callback,
                payload: action.payload,
                sequence: state.sequence + 1
                
            };
        default:
            return state;
    }
}
