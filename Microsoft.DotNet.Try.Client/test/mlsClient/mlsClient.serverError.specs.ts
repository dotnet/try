// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import IMlsClient from "../../src/IMlsClient";
import MlsClient from "../../src/MlsClient";

import { emptyWorkspace } from "../testResources";
import { suite } from "mocha-typescript";
import { failingFetcher } from "../Utility/failingFetcher";
import { uriThat404s } from "./constantUris";
import { NullAIClient } from "../../src/ApplicationInsights";

suite("MlsClient server error specs", () => {
    let client: IMlsClient;

    beforeEach(async function () {
        client = new MlsClient(
            failingFetcher,
            uriThat404s,
            () => undefined,
            new NullAIClient(),
            uriThat404s);
    });

    let testCases = [{
        name: "acceptCompletionItem",
        invoke: async (client: IMlsClient) => client.acceptCompletionItem({
            acceptanceUri: `/workspace/acceptCompletionItem?listId=7fc72521-7293-4781-affa-041f787cfe8e&index=0`,
            detail: "",
            documentation: "",
            filterText: "",
            insertText: "",
            kind: 0,
            label: "",
            sortText: "",
        })
    }, {
        name: "run",
        invoke: async (client: IMlsClient) => client.run({ workspace: emptyWorkspace })
    }, {
        name: "getCompletionList",
        invoke: async (client: IMlsClient) => client.getCompletionList(null, "", 0, "roslyn")
    }, {
        name: "getSignatureHelp",
        invoke: async (client: IMlsClient) => client.getSignatureHelp(null, "", 0)
    }, {
        name: "getSourceCode",
        invoke: async (client: IMlsClient) => client.getSourceCode({ sourceUri: "" })
    }
    ];

    testCases.forEach(c =>
        it(`${c.name} throws an Error when resource is not found`, async () => {
            return c.invoke(client).should.eventually.be.rejectedWith(Error, "Not Found");
        })
    );
});
