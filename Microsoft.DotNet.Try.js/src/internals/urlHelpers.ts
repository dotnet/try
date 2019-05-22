// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Configuration } from "../configuration";

export function generateEditorUrl(configuration: Configuration, messageBusId: string, packageName?: string): string {
    const host = configuration.trydotnetOrigin ? configuration.trydotnetOrigin : "https://try.dot.net";
    let url = new URL(host);
    url.pathname = "v2/editor";

    url.searchParams.append("waitForConfiguration", "true");
    if (messageBusId) {
        url.searchParams.append("editorId", messageBusId);
    }

    if(!!configuration.debug === true){
        url.searchParams.append("debug", "true");
    }

    if(!!configuration.useBlazor === true){
        url.searchParams.append("useBlazor", "true");
    }

    buildQueryString(url, packageName);
    return url.href;
}

function buildQueryString(url: URL, packageName?: string) {
    if (packageName) {
        url.searchParams.append("workspaceType", packageName);
    }
}

export function extractTargetOriginFromIFrame(iframe: HTMLIFrameElement): string {
    let origin = "";
    let src = iframe.getAttribute("src");
    if (src) {
        let url = new URL(src);
        origin = url.origin;
    }
    return origin;
}
