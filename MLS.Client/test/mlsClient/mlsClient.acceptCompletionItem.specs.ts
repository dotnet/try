// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import IMlsClient from "../../src/IMlsClient";
import ICanGetAClient from "./ICanGetAClient";
import ICompletionItem from "../../src/ICompletionItem";

export default (getClient: ICanGetAClient) => {
    describe(`acceptCompletionItem`, function() {
        let client: IMlsClient;

        beforeEach(async function() {
            client = await getClient();
        });

        it("can send a completion acceptance notification", async function () {
            let acceptedItem: ICompletionItem = {
                acceptanceUri: `/workspace/acceptCompletionItem?listId=7fc72521-7293-4781-affa-041f787cfe8e&index=0`,
                detail: "",
                documentation: "",
                filterText: "",
                insertText: "",
                kind: 0,
                label: "",
                sortText: "",
            };

            return client.acceptCompletionItem(acceptedItem)
                .should.eventually.be.fulfilled;
        });

        it("does not send a completion acceptance notification if there is no uri", async function () {
            let acceptedItem: ICompletionItem = {
                detail: "",
                documentation: "",
                filterText: "",
                insertText: "",
                kind: 0,
                label: "",
                sortText: "",
            };

            return client.acceptCompletionItem(acceptedItem)
                .should.eventually.be.fulfilled;
        });
    });
};
