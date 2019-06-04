// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AnyAction } from "redux";

export class ReduxCookie {
    public static Create(version: string, actions: AnyAction[]): ReduxCookie {
        var filteredActions = actions.filter(action => action !== null);
        if (filteredActions.length > 0) {
            return new ReduxCookie(version, filteredActions);
        }

        return null;
    }

    constructor(public version: string, public actions: AnyAction[]) {
    }
}
