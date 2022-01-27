// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ICanFetch } from "../../src/MlsClient";
import chai = require("chai");
import fetcher from "../../src/utilities/fetch";
import { uriThatECONNREFUSEDs, uriThat404s, uriThatENOTFOUNDs } from "../mlsClient/constantUris";
import { failingFetcher } from "./failingFetcher";

chai.use(require("chai-as-promised"));
chai.use(require("chai-subset"));
chai.should();

export interface ICanGetAnICanFetch {
    (): ICanFetch;
}

export default function ICanFetchSpecs(getICanFetch: ICanGetAnICanFetch, implementation: string) {
    describe(`${implementation} implements ICanFetch`, () => {
        let iCanFetch: ICanFetch;

        beforeEach(async function () {
            iCanFetch = getICanFetch();
        });

        it("throws ENOTFOUND when fetching from a nonexistent host", async function () {
            return iCanFetch(uriThatENOTFOUNDs.href, null).should.eventually.be.rejectedWith(Error, "ENOTFOUND");
        });

        it("throws ECONNREFUSED when fetching from a nonexistent IP address", async function () {
            return iCanFetch(uriThatECONNREFUSEDs.href, null).should.eventually.be.rejectedWith(Error, "ECONNREFUSED");
        });

        it("returns 404 when valid domain has no matching path", async function () {
            let response = await iCanFetch(uriThat404s.href, null);
            
            // tslint:disable-next-line:no-unused-expression-chai
            response.ok.should.be.false;
            response.status.should.be.equal(404);
            response.statusText.should.be.equal("Not Found");
        });
    });
}

describe.skip("ICanFetch integration tests", async function () {
    this.timeout(10000);


    if (process.env.MLS_RUN_SIMULATOR_TESTS === "true") {
        ICanFetchSpecs(() => fetcher, "fetcher");
    }
});

describe.skip("ICanFetch failing simulator tests", async function () {
    this.timeout(10000);

    ICanFetchSpecs(() => failingFetcher, "failingFetcher");
});

