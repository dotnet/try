// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as types from "../../src/constants/ActionTypes";

import MlsClientSimulator from "../mlsClient/mlsClient.simulator";
import actions from "../../src/actionCreators/actions";
import getStore from "../observableAppStore";

describe("CONFIGURE Action Creators", () => {
    it("should set a client", () => {
        const myClient = new MlsClientSimulator(new URL("https://try.dot.net"));

        const expectedAction = {
            type: types.CONFIGURE_CLIENT,
            client: myClient
        };

        actions.setClient(myClient).should.deep.equal(expectedAction);
    });

    it("should set code source", () => {
        const expectedAction = {
            type: types.CONFIGURE_CODE_SOURCE,
            from: "myCodeSource",
            sourceCode: "mySourceCode"
        };

        actions.setCodeSource("myCodeSource", "mySourceCode").should.deep.equal(expectedAction);
    });

    it("should set hosting domain", () => {
        const expectedAction = {
            type: types.NOTIFY_HOST_PROVIDED_CONFIGURATION,
            configuration: { hostOrigin: new URL("https://try.dot.net") }
        };

        actions.notifyHostProvidedConfiguration({ hostOrigin: new URL("https://try.dot.net") }).should.deep.equal(expectedAction);
    });

    it("should hide the editor", () => {
        const expectedAction = {
            type: types.HIDE_EDITOR
        };

        actions.hideEditor().should.deep.equal(expectedAction);
    });

    it("should enable preview", () => {
        const expectedAction = {
            type: types.CONFIGURE_ENABLE_PREVIEW
        };

        actions.enablePreviewFeatures().should.deep.equal(expectedAction);
    });

    it("should enable instrumentation", () => {
        const store = getStore();
        store.dispatch(actions.enableInstrumentation());
        store.getActions().should.deep.members([{ type: types.CONFIGURE_ENABLE_INSTRUMENTATION }]);
    });
});
