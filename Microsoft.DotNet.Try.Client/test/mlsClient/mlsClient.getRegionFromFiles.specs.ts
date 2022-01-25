// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import IMlsClient from "../../src/IMlsClient";

import chai = require("chai");
import { createMockHttpServer, IMockHttpServer } from "./mockHttpServerFactory";
import ICanGetAClient from "./ICanGetAClient";
import { CreateRegionsFromFilesResponse } from "../../src/clientApiProtocol";


chai.use(require("chai-as-promised"));
chai.use(require("chai-subset"));
chai.should();

export default (getClient: ICanGetAClient) => {
    describe.skip(`getRegionsFromFiles`, () => {
        let server: IMockHttpServer;

        let client: IMlsClient;

        beforeEach(async function () {
            server = await createMockHttpServer("localhost");
            await server.start();
            client = await getClient();
        });

        it("can extract region for a file list", async function () {
            const expectedResponse: CreateRegionsFromFilesResponse = {
                requestId: "testRun",
                regions: [{ id: "file.cs@get", content: "this part" }]
            };
            server.on({
                method: "POST",
                path: "/project/files/regions",
                reply: {
                    status: 200,
                    body: function () {
                        return expectedResponse;
                    }
                }
            });


            let result = await client.createRegionsFromProjectFiles({ requestId: "testRun", files: [{ name: "file.cs", content: "get$$this part" }] });

            result.requestId.should.be.equal("testRun");
            result.regions[0].id.should.be.equal("file.cs@get");
            result.regions[0].content.should.be.equal("this part");

        });

        afterEach(async function () {
            await server.stop();
        });
    });
};
