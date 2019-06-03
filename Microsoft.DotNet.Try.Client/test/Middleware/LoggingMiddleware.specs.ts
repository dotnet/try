// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { applyMiddleware, createStore } from "redux";

import { Action } from "redux";
import getMiddlewareForLogger from "../../src/middleware/LoggingMiddleware";

require("jsdom-global")();

describe("LoggingMiddleware", () => {
    it("reports actions through the provided logger", () => {
        const dispatchedActions = [
            { type: "SOME_ACTION" },
            { type: "ANOTER_ACTION_WITH_PROPS", prop: "value" }
        ];
        const loggedActions: Action[] = [];

        var logger = (m: Action) => {
            loggedActions.push(m);
        };

        window.addEventListener("message", logger);

        const store = createStore(() => { }, applyMiddleware(getMiddlewareForLogger(logger)));

        store.dispatch(dispatchedActions[0]);
        store.dispatch(dispatchedActions[1]);

        loggedActions.should.deep.equal(dispatchedActions);
    });

    it("does not crash if window.postMessage is not defined", () => {
        Reflect.deleteProperty(global, "window");

        const dispatchedActions = [
            { type: "SOME_ACTION" },
            { type: "ANOTER_ACTION_WITH_PROPS", prop: "value" }
        ];
        const store = createStore(() => { }, applyMiddleware(getMiddlewareForLogger(() => { })));

        store.dispatch(dispatchedActions[0]);
        store.dispatch(dispatchedActions[1]);
    });
});
