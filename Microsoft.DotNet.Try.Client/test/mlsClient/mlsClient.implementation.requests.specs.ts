// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";
import MlsClient from "../../src/MlsClient";
import fetch from "../../src/utilities/fetch";
import {defaultWorkspace, clientConfigurationExample, emptyWorkspace} from "../testResources";
import { IWorkspaceFile } from "../../src/IState";
import { IMockHttpServer, createMockHttpServer } from "./mockHttpServerFactory";
import { NullAIClient } from "../../src/ApplicationInsights";

chai.should();

describe.skip("MlsClient Implementation", function() {
  let server: IMockHttpServer;
  beforeEach(async function() {
    server = await createMockHttpServer("localhost");
    await server.start();
    addConfigurationResponseTo(server);
  });

  it(`sends 'content-type: application/json' to '/workspace/run'`, async () => {
    addRunResponseTo(server);

    var client = new MlsClient(fetch, 
                               new URL(`https://dot.net`), 
                               null, 
                               new NullAIClient(), 
                               new URL(server.url)); 
 
    await client.run({ workspace: emptyWorkspace }); 
    
    server.requests()[0].headers.should.include.keys("content-type");
    server.requests()[0].headers["content-type"].should.equal("application/json");
  });

  it(`sends 'hostOrigin' query string value to '/workspace/run'`, async () => {
    addRunResponseTo(server);
    var client = new MlsClient(fetch, 
                               new URL(`https://dot.net`),
                               null, 
                               new NullAIClient(), 
                               new URL(server.url));

    const workspace = {
      ...defaultWorkspace,
      files: [] as IWorkspaceFile[],
      buffer: "",
      type: "script"
    };
    await client.run({ workspace });
    let requests = requestsToRelativeUrls(server);
    requests.should.contain("/workspace/run?hostOrigin=https%3A%2F%2Fdot.net%2F");
  });

  it(`sends 'hostOrigin' query string value to '/snippet'`, async () => {
    addSnippetResponseTo(server);
    var client = new MlsClient(fetch, 
                               new URL(`https://dot.net`), 
                               null, 
                               new NullAIClient(), 
                               new URL(server.url));

    await client.getSourceCode({ sourceUri: "https://dot.net" });
    let requests = requestsToRelativeUrls(server);
    requests.should.contain("/snippet?hostOrigin=https%3A%2F%2Fdot.net%2F&from=https%3A%2F%2Fdot.net");
  });

  it(`sends 'XSRF-TOKEN' header if it has a value present in the cookie`, async () => {
    addSnippetResponseTo(server);
    var client = new MlsClient(fetch, 
                               new URL(`https://dot.net`),
                               (s: string) => (s === "XSRF-TOKEN" ? "abc123" : null), 
                               new NullAIClient(), 
                               new URL(server.url));

    await client.getSourceCode({ sourceUri: "https://dot.net" });
    server.requests()[0].headers["xsrf-token"].should.equal("abc123");
  });

  it(`does not send 'XSRF-TOKEN' header if it does not have a value present in the cookie`, async () => {
    addSnippetResponseTo(server);
    var client = new MlsClient(fetch, 
                               new URL(`https://dot.net`), 
                               () => null, 
                               new NullAIClient(), 
                               new URL(server.url));

    await client.getSourceCode({ sourceUri: "https://dot.net" });
    server.requests()[0].headers.should.not.contain.key("XSRF-TOKEN");
  });

  afterEach(async () => {
    await server.stop();
  });
});

function addRunResponseTo(server: IMockHttpServer) {
  server.on({
    method: "POST",
    path: "/workspace/run",
    reply: {
      status: 200,
      body: function() {
        return "{}";
      }
    }
  });
}

function addConfigurationResponseTo(server: IMockHttpServer) {
  server.on({
    method: "POST",
    path: "/clientConfiguration",
    reply: {
      status: 200,
      body: function() {
        return JSON.stringify(clientConfigurationExample);
      }
    }
  });
}

function addSnippetResponseTo(server: IMockHttpServer) {
  server.on({
    method: "GET",
    path: "/snippet",
    reply: {
      status: 200,
      body: function() {
        return "{}";
      }
    }
  });
}

function requestsToRelativeUrls(server: IMockHttpServer) {
  let requests = server.requests().map((r: any) => (<string>r.url).replace(server.url, ""));
  return requests;
}
