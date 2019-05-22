// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as types from "../constants/ActionTypes";
import { Action } from "../constants/ActionTypes";
import { ThunkDispatch } from "redux-thunk";
import { AnyAction } from "redux";
import IState from "../IState";

export function canShowGitHubPanel(canShow: boolean): Action {
    return {
        type: types.CAN_SHOW_GITHUB_PANEL,
        canShow
    };
}

export function disableBranding(): Action {
    return {
        type: types.CONFIGURE_BRANDING,
        visible: false
    };
}

export function enableBranding(): Action {
    return {
        type: types.CONFIGURE_BRANDING,
        visible: true
    };  
}

export function configureBranding() {
    return async (dispatch: ThunkDispatch<IState, void, AnyAction>, getState: () => IState): Promise<Action> => {
        const state = getState();     
        let client = state.config.client;
        let enableBanner = true;
        let configuration = await client.getConfiguration();
        enableBanner = configuration.enableBranding;
        let actionToDispatch = enableBanner === true 
            ? enableBranding() 
            : disableBranding();     
        return dispatch(actionToDispatch);
    };
}

