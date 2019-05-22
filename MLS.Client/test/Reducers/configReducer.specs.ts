// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { defaultWorkspace, fibonacciCode } from "../testResources";

import MlsClientSimulator from "../mlsClient/mlsClient.simulator";
import actions from "../../src/actionCreators/actions";
import reducer from "../../src/reducers/configReducer";
import { IApplicationInsightsClient, NullAIClient } from "../../src/ApplicationInsights";

describe("config Reducer", () => {
    it("should return the initial state", () => {
        reducer(undefined, undefined).should.deep.equal({
            client: undefined,
            completionProvider: "roslyn",
            from: "default",
            hostOrigin: undefined,
            defaultWorkspace: { ...defaultWorkspace },
            defaultCodeFragment: fibonacciCode,
            version: 1,
            applicationInsightsClient: undefined
        });
    });

    it("should handle CONFIGURE_CODE_SOURCE and update state", () => {
        const originalState = { salt: 1 };
        const action = actions.setCodeSource("a source", "some code");
        const result = {
            salt: 1,
            from: "a source",
            defaultCodeFragment: "some code"
        };

        reducer(originalState, action)
            .should.deep.equal(result).and.not.equal(originalState);
    });

    it("should handle CONFIGURE_CLIENT and update state", () => {
        const client = new MlsClientSimulator(new URL("https://a_fake_url.fake"));
        const originalState = { salt: 1 };
        const action = actions.setClient(client);
        const result = {
            salt: 1,
            client
        };

        reducer(originalState, action)
            .should.deep.equal(result).and.not.equal(originalState);
    });

    it("should handle CONFIGURE_VERSION and update state", () => {
        const originalState = { salt: 1 };
        const action = actions.setVersion(42);
        const result = {
            salt: 1,
            version: 42
        };

        reducer(originalState, action)
            .should.deep.equal(result).and.not.equal(originalState);
    });

    it("should handle CONFIGURE_COMPLETION_PROVIDER and update state", () => {
        const originalState = { salt: 1 };
        const action = actions.setCompletionProvider("pythia");
        const result = {
            salt: 1,
            completionProvider: "pythia"
        };

        reducer(originalState, action)
            .should.deep.equal(result).and.not.equal(originalState);
    });

    it("should handle ENABLE_TELEMETRY and update state", () => {
        const originalState = { salt: 1 };
        var client: IApplicationInsightsClient = new NullAIClient()
        const action = actions.enableClientTelemetry(client);
        const result = {
            salt: 1,
            applicationInsightsClient: client
        };
        reducer(originalState, action)
            .should.deep.equal(result).and.not.equal(originalState);
    });
});
