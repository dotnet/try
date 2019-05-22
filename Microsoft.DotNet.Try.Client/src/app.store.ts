// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { applyMiddleware, createStore } from "redux";

import { Middleware } from "redux";
import rootReducer from "./reducers/app.reducer";
import thunk from "redux-thunk";
import { IStore } from "./IStore";

export default (additionalMiddleware: Middleware[] = []) : IStore => {
    var middleware = [
        thunk,
        ...additionalMiddleware
    ];

    return createStore(rootReducer, undefined, applyMiddleware(...middleware));
};
