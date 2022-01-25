// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import MlsClient, { Request } from "../../src/MlsClient";

import FetchWithCookies from "./fetchWithCookies";
import baseFetch from "../../src/utilities/fetch";
import mlsClientSpecs from "./mlsClient.specs";
import { baseAddress } from "./constantUris";
import { NullAIClient } from "../../src/ApplicationInsights";

describe.skip("MlsClient Implementation", async function () {
    this.timeout(10000);

    if (process.env.MLS_RUN_SIMULATOR_TESTS === "true") {
        let fetcher = new FetchWithCookies(baseFetch, baseAddress);

        let getClient =
            async function () {
                try {
                    await fetcher.fetch(`${baseAddress}/`, {
                        method: "GET",
                        headers: {}
                    });
                }
                catch (e) {
                    console.log(baseAddress);
                    console.log(`Remember to start Orchestrator on port ${baseAddress}!`);
                    console.log(e);
                }

                return new MlsClient(
                    async (uri: string, request: Request) => fetcher.fetch(uri, request),
                    baseAddress,
                    (key: string) => fetcher.CookieGetter(key),
                    new NullAIClient(),
                    baseAddress);
            };

        mlsClientSpecs(getClient, "implementation");
    }
});
