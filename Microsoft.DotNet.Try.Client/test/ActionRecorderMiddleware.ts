// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Action, Middleware } from "redux";

export default (recordedActions: Action[]): Middleware =>
    () => (next) => (action) => {
        recordedActions.push(action);
        return next(action);
    };
