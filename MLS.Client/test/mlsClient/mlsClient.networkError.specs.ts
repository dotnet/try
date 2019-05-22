// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";

import IMlsClient from "../../src/IMlsClient";

import { emptyWorkspace } from "../testResources";
import { getNetworkFailureClient } from "./getNetworkFailureClient";

chai.use(require("chai-as-promised"));
chai.should();

describe("MlsClient network error specs", async function () {
    this.timeout(10000);
    let client: IMlsClient;

    beforeEach(function () {
        client = getNetworkFailureClient();
    });

    let testCases = [{
        name: "acceptCompletionItem",
        invoke: async (client: IMlsClient) => client.acceptCompletionItem({
            acceptanceUri: "/workspace/acceptCompletionItem?listId=7fc72521-7293-4781-affa-041f787cfe8e&index=0",
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
        it(`${c.name} throws an Error when connection is refused`, async () => {
            return c.invoke(client)
                .should.eventually.be.rejectedWith(Error, "ECONNREFUSED");
        })
    );
});
