// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";
import { expect } from "chai";
import fetch from "../../src/utilities/fetch"; 
import { IMockHttpServer, createMockHttpServer } from "./mockHttpServerFactory";
import { ClientConfiguration, ApiRequest, extractApiRequest, ApiParameters } from "../../src/clientConfiguration";
import { clientConfigurationExample, emptyWorkspace } from "../testResources";
import MlsClient from "../../src/MlsClient";
import { NullAIClient } from "../../src/ApplicationInsights";

chai.should();

describe("Client Configuration manipulation", () => {

    let server: IMockHttpServer;
    beforeEach(async function () {
        server = await createMockHttpServer("localhost");
        await server.start();        
        addConfigurationResponseTo(server);
    });  

    afterEach(async () => {
        await server.stop();
    });

    describe("Given a client configuration", () => {
        [{
            label: "loadFrom",
            apiKey: "snippet",
            apiParameters: { from: "https://url.for.code" },
            hostOrigin: "http://this.side.com",
            expectedApi: {
                url: new URL(`https://try.dot.net/snippet?hostOrigin=http%3A%2F%2Fthis.side.com%2F&from=https%3A%2F%2Furl.for.code`),
                timeoutMsHeader: "15000",
                method: "GET"
            }
        },
        {
            label: "loadFromGist",
            apiKey: "loadFromGist",
            apiParameters: { gistId: "customGist" },
            expectedApi: {
                url: new URL(`https://try.dot.net/workspace/fromgist/customGist`),
                timeoutMsHeader: "15000",
                method: "GET"
            }
        },
        {
            label: "loadFromGist with hash",
            apiKey: "loadFromGist",
            apiParameters: { gistId: "customGist", commitHash: "commitHashValue" },
            expectedApi: {
                url: new URL(`https://try.dot.net/workspace/fromgist/customGist/commitHashValue`),
                timeoutMsHeader: "15000",
                method: "GET"
            }
        },
        {
            label: "loadFromGist with hash and parameters",
            apiKey: "loadFromGist",
            apiParameters: { gistId: "customGist", commitHash: "commitHashValue", workspaceType: "nodatime.api" },
            expectedApi: {
                url: new URL(`https://try.dot.net/workspace/fromgist/customGist/commitHashValue?workspaceType=nodatime.api`),
                timeoutMsHeader: "15000",
                method: "GET"
            }
        }].forEach(test => {
            it(`can generates api request for ${test.label}`, () => {
                const configuration = clientConfigurationExample;
                let apiCall = extractApiCallFromConfiguration(
                    configuration, 
                    test.apiKey, 
                    test.apiParameters, 
                    test.hostOrigin);
                
                expect(apiCall).to.deep.equal(test.expectedApi);
            });
        });

        it("the client appends 'ClientConfigurationVersionId' header to the request", async () => {
            addRunResponseTo(server);
     
            var client = new MlsClient(fetch, new URL("https://try.dot.net"), null, new NullAIClient(), new URL(server.url)); 
     
            await client.run({ workspace: emptyWorkspace }); 
     
            let requests = <any[]>server.requests();
            requests[requests.length - 1].headers.should.include.keys("clientconfigurationversionid");
        });
    
        it("the client appends 'Timeout' header to the request", async () => {
            addRunResponseTo(server);

            var client = new MlsClient(fetch, new URL("https://try.dot.net"), null, new NullAIClient(), new URL(server.url)); 
     
            await client.run({ workspace: emptyWorkspace }); 
     
            let requests = <any[]>server.requests();
            requests[requests.length - 1].headers.should.include.keys("timeout");
            requests[requests.length - 1].headers["timeout"].should.be.equal("15000");
        });
    
    });
});

function addRunResponseTo(server: any) {
    server.on({
        method: "POST",
        path: "/workspace/run",
        reply: {
            status: 200,
            body: function () {
                return "{}";
            }
        }
    });
}

function addConfigurationResponseTo(server: any) {
    server.on({
        method: "POST",
        path: "/clientConfiguration",
        reply: {
            status: 200,
            body: function () {
                return JSON.stringify(clientConfigurationExample);
            }
        }
    });
}

function extractApiCallFromConfiguration(
    configuration: ClientConfiguration,
    apiKey: string,
    apiParameters: ApiParameters,
    hostOrigin?: string
  ): ApiRequest {
    let requestDescriptor = configuration._links[apiKey];
    return extractApiRequest(
        requestDescriptor, 
        apiParameters, 
        hostOrigin ? new URL(hostOrigin) : null,
        new URL("https://try.dot.net"));
}
