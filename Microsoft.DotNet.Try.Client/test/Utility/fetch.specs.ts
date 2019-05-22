// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";

import {Request} from "../../src/MlsClient";
import fetch from "../../src/utilities/fetch";
import {suite} from "mocha-typescript";
import { createMockHttpServer, IMockHttpServer } from "../mlsClient/mockHttpServerFactory";

chai.use(require("chai-as-promised"));
chai.should();

suite("fetch", () => {    
    let server : IMockHttpServer;

    beforeEach(async function () {
        server = await createMockHttpServer("localhost");
        await server.start();
    });

    it("throws if the remote server is unavailable Promisely", (done) => {
        var request: Request =
        {
            method: "GET",
            headers: {}
        };
        
        fetch(`http://localhost:12399}/index.html`, request)
            .then(() => done("fetch should have thrown"))
            .catch(_e => { done(); });
    });

    it("throws if the remote server is unavailable Asyncly", async () => {
        var request: Request =
        {
            method: "GET",
            headers: {}
        };
        
        let threw = true;

        try {
            await fetch(`http://localhost:12399}/index.html`, request);

            threw = false;
        }
        catch (e) {
        }

        if (!threw) {
            throw new Error(":("); 
        }
    });

    it("throws if the remote server is unavailable Chaily", async () => {
        var request: Request =
        {
            method: "GET",
            headers: {}
        };
        
        return fetch(`http://localhost:12399}/index.html`, request).should.eventually.be.rejected; 
    });

    it("can GET from mock http server", async () => {
        server.on({
                method: "GET",
                path: "/index.html",
                reply: {
                    status: 200,
                    body: function () {
                        return "<html></html>";
                    }
                }
            });
        
        var request: Request =
        {
            method: "GET",
            headers: {}
        };

        let result = await  fetch(`${server.url}/index.html`, request);
        
        result.ok.should.be.equal(true);
        (await result.text()).should.be.equal("<html></html>");
    });

    afterEach(async () => {
        await server.stop();
    });
});
