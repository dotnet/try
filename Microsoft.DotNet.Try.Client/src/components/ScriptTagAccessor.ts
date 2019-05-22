// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ClientParameters } from "./BrowserAdapter";

export class ScriptTagAccessor {
  constructor(private document?: HTMLDocument) {
    if (!this.document) {
      this.document = document;
    }
  }
  public getClientParameters(): ClientParameters {
    let bundleJs = this.document.querySelector("script[id=bundlejs]");
    if (bundleJs) {
      let additionalParameters = bundleJs.getAttribute("data-client-parameters");
      if (additionalParameters) {
        let parsed = JSON.parse(additionalParameters);
        if(parsed.referrer !== undefined && parsed.referrer !== null){
          let referrerUrl = new URL(parsed.referrer) ;
          parsed =  { ...parsed, referrer: referrerUrl};
        }
        return parsed;
      }
    }
    return {};
  }
}
