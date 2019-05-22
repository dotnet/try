// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as types from "../constants/ActionTypes";
import { Action } from "../constants/ActionTypes";
import { IBlazorState } from "../IState";



const initialState: IBlazorState = {
    callback: undefined,
    payload: undefined,
    sequence: 0
};

export default function blazorReducer(state: IBlazorState = initialState, action: Action): IBlazorState {
    switch (action.type) {
        case types.SEND_BLAZOR_MESSAGE:
            return {
                callback: action.callback,
                payload: action.payload,
                sequence: state.sequence + 1
                
            };
        default:
            return state;
    }
}
