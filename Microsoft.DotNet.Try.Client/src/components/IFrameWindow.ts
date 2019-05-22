// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IIFrameWindow, ClientParameters } from "./BrowserAdapter";
import { ScriptTagAccessor } from "./ScriptTagAccessor";

export class IFrameWindow implements IIFrameWindow {
    private _apiBaseAddress: URL;
    private _hostOrigin: URL;
    private _query: URLSearchParams;
    private _clientParameters: ClientParameters;

    constructor(private iframeWindow: Window) {
        this._apiBaseAddress = new URL(new URL(this.iframeWindow.location.href).origin);
        this._query = new URLSearchParams(this.iframeWindow.location.search);

        let scriptTagAccessor = new ScriptTagAccessor(this.iframeWindow.document);
        this._clientParameters = scriptTagAccessor.getClientParameters();

        this._hostOrigin = this._query.has("hostOrigin")
            ? new URL(this._query.get(decodeURIComponent("hostOrigin")))
            : this._clientParameters.referrer;
    }

    public getApiBaseAddress(): URL {
        return this._apiBaseAddress;
    }

    public getClientParameters(): ClientParameters {
        return this._clientParameters;
    }

    public addEventListener(type: string, listener: (message: MessageEvent) => void): void {
        this.iframeWindow.addEventListener(type, listener);
    }

    public getQuery(): URLSearchParams {
        return this._query;
    }

    public getReferrer(): URL {
        return this._clientParameters.referrer;
    }
    
    public getHostOrigin(): URL {
        return this._hostOrigin;
    }
}
