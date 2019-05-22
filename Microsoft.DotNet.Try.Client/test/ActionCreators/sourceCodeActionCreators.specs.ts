// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";
import * as types from "../../src/constants/ActionTypes";
import getStore, { IObservableAppStore } from "../observableAppStore";
import MlsClientSimulator from "../mlsClient/mlsClient.simulator";
import actions from "../../src/actionCreators/actions";
import { fibonacciCode } from "../testResources";
import { uriThat404s } from "../mlsClient/constantUris";
import { getNetworkFailureClient } from "../mlsClient/getNetworkFailureClient";

chai.should();
chai.use(require("chai-exclude"));

describe("sourceCode Action Creators", () => {
    var store: IObservableAppStore;

    beforeEach(() => {
        store = getStore();
    });

    it("should create an action to load source", () => {
        const expectedAction = {
            type: types.LOAD_CODE_REQUEST,
            from: "somewhere"
        };

        actions.loadCodeRequest("somewhere").should.deep.equal(expectedAction);
    });

    it("should create an action to set usings", () => {
        const expectedAction = {
            type: types.SET_ADDITIONAL_USINGS,
            additionalUsings: ["a_using"]
        };

        actions.setAdditionalUsings(["a_using"]).should.deep.equal(expectedAction);
    });

    it("should create an action to process load source success", () => {
        var sourceCode = "1 + 1";

        const expectedAction = {
            type: types.LOAD_CODE_SUCCESS,
            sourceCode,
            bufferId: "Program.cs"
        };

        actions.loadCodeSuccess(sourceCode, "Program.cs").should.deep.equal(expectedAction);
    });

    it("should create an action to process load source failure", () => {
        var ex = new Error("404");

        const expectedAction = {
            type: types.LOAD_CODE_FAILURE,
            ex
        };

        actions.loadCodeFailure(ex).should.deep.equal(expectedAction);
    });

    it("creates LOAD_CODE_REQUEST and LOAD_CODE_SUCCESS when fetching default sourceCode",async () => {
        const expectedActions = [
            actions.loadCodeRequest("default"),
            actions.loadCodeSuccess(fibonacciCode, "Program.cs")
        ];

        await store.dispatch(actions.loadSource());
        return  store.getActions().should.deep.equal(expectedActions);
    });

    it("creates LOAD_CODE_REQUEST and LOAD_CODE_SUCCESS when fetching sourceCode from parameter", async () => {
        const expectedActions = [
            actions.loadCodeRequest("parameter"),
            actions.loadCodeSuccess("some source code", "Program.cs")
        ];

        store.configure([
            actions.setCodeSource("parameter", "some source code")
        ]);

        await store.dispatch(actions.loadSource());
        return store.getActions().should.deep.equal(expectedActions);
    });

    it("creates LOAD_CODE_REQUEST and LOAD_CODE_SUCCESS when fetching sourceCode from Uri", async () => {
        const expectedActions = [
            actions.loadCodeRequest("some/source/file.cs"),
            actions.loadCodeSuccess("Console.WriteLine(\"Hello, World\");", "Program.cs")
        ];

        store.configure([
            actions.setClient(new MlsClientSimulator()),
            actions.setCodeSource("some/source/file.cs")
        ]);

        await store.dispatch(actions.loadSource());
        store.getActions().should.deep.equal(expectedActions);
    });

    it("creates LOAD_CODE_REQUEST and LOAD_CODE_FAILURE when fetching sourceCode from Uri fails", async () => {
        const expectedActions = [
            actions.loadCodeRequest(""),
            actions.loadCodeFailure(new Error("404"))
        ];

        store.configure([
            actions.setClient(getNetworkFailureClient(uriThat404s)),
            actions.setCodeSource("")
        ]);

        await store.dispatch(actions.loadSource());
        store.getActions().should.excludingEvery("ex").deep.equal(expectedActions);
    });
});
