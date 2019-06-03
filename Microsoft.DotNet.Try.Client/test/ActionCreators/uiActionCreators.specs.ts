// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";
import getStore, { IObservableAppStore } from "../observableAppStore";
import actions from "../../src/actionCreators/actions";

chai.should();
chai.use(require("chai-exclude"));

describe("ui Action Creators", () => {
    var store: IObservableAppStore;

    beforeEach(() => {
        store = getStore();
    });

    it("should enable github panel", () => {
        const expectedActions = [
            actions.canShowGitHubPanel(true)
        ];

        store.configure([actions.canShowGitHubPanel(false)]);
        store.dispatch(actions.canShowGitHubPanel(true));

        store.getActions().should.deep.equal(expectedActions);
    });

    it("should disable github panel", () => {
        const expectedActions = [
            actions.canShowGitHubPanel(false)
        ];
        store.configure([actions.canShowGitHubPanel(true)]);
        store.dispatch(actions.canShowGitHubPanel(false));

        store.getActions().should.deep.equal(expectedActions);
    });

});
