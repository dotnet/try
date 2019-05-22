// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const fp = require("find-free-port");
let MockHttpServer = require("mock-http-server");

export interface IMockHttpServer {
    port: number;
    url: string;
    start: () => Promise<void>;
    stop: () => Promise<void>;
    on: (stuff: any) => void;
    [propname: string]: any;
}

export async function createMockHttpServer(host: string = "localhost") : Promise<IMockHttpServer> {
    return fp(10000, 20000).then(([freep]: number[]) => {
        let port = freep;
        let serverUrl = `http://${host}:${port}`;
        let server = new MockHttpServer({ host: host, port: port });
        let start = server.start;
        let stop = server.stop;
        server.port = port;
        server.url = serverUrl;
        server.start = async () => new Promise(async (resolve) => start(resolve));
        server.stop = async () => new Promise(async (resolve) => stop(resolve));
        return server;        
    });
}

