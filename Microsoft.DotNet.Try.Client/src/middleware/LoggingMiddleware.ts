// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AnyAction, Middleware } from "redux";

export type ActionLogger = (s: AnyAction) => void;

export default (logger: ActionLogger): Middleware =>
    () => (next) => (action) => {
        logger(action);
        return next(action);
    };
