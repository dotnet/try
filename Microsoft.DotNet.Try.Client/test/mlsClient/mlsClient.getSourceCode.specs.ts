// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import IMlsClient from "../../src/IMlsClient";
import chai = require("chai");
import { IMockHttpServer, createMockHttpServer } from "./mockHttpServerFactory";
import ICanGetAClient from "./ICanGetAClient";

chai.use(require("chai-as-promised"));
chai.use(require("chai-subset"));
chai.should();

export default (getClient: ICanGetAClient) => {
    describe.skip(`getSourceCode`, () => {
        let server: IMockHttpServer;
        let client: IMlsClient;

        beforeEach(async function () {
            server = await createMockHttpServer("localhost");
            await server.start();
            client = await getClient();
        });

        it("can retrieve source code from url", async function () {
            let expectedDisplayedCode = "Console.WriteLine(\"Hello, World\");";

            server.on({
                method: "GET",
                path: "/some/source/file.cs",
                reply: {
                    status: 200,
                    body: function () {
                        return expectedDisplayedCode;
                    }
                }
            });

            let sourceUri = `${server.url}/some/source/file.cs`;

            let result = await client.getSourceCode({ sourceUri: sourceUri });

            result.buffer.should.be.equal(expectedDisplayedCode);
        });

        afterEach(async function () {
            await server.stop();
        });
    });
};
