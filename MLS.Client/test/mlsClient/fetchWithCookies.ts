// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

let fetchCookie = require("fetch-cookie");
import { ICanFetch, Request, Response } from "../../src/MlsClient";
import { CookieJar } from "tough-cookie";

export default class FetchWithCookies {
    private baseFetch: ICanFetch;
    private cookieJar: CookieJar;
    private baseUrl: URL;

    constructor(fetch: ICanFetch, baseUrl: URL) {
        this.cookieJar = new CookieJar();
        this.baseUrl = baseUrl;
        this.baseFetch = fetchCookie(fetch, this.cookieJar);
        this.CookieGetter = this.CookieGetter.bind(this);
        this.fetch = this.fetch.bind(this);
        this.fetch = this.fetch.bind(this);
    }

    public async fetch(uri: string, request: Request): Promise<Response> {
        let response = await this.baseFetch(uri, request);
        
        return response;       
    }

    public CookieGetter(key: string){
        let value = this.GetCookieValue(this.baseUrl, key);
        return value;
    }

    private GetCookieValue(currentUrl: URL, key: string){
        let cookie = this.cookieJar.getCookiesSync(currentUrl.href).find(c => c.key === key);
        
        return cookie
            ? cookie.value
            : undefined;
    }
}
