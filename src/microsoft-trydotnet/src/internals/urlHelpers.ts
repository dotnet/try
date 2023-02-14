// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as polyglotNotebooks from "@microsoft/polyglot-notebooks";
import { Configuration } from "../configuration";

export function generateEditorUrl(configuration: Configuration, packageName?: string): string {
    polyglotNotebooks.Logger.default.info(`${JSON.stringify(configuration)}`);
    const host = configuration.trydotnetOrigin ? configuration.trydotnetOrigin : "https://try.dot.net";
    let url = new URL(host);
    url.pathname = "/editor";

    url.searchParams.append("waitForConfiguration", "true");

    url.searchParams.append("editorId", "-0-");
    if (configuration.enableLogging) {
        url.searchParams.append("enableLogging", "true");
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
    let origin = "*";
    let src = iframe.getAttribute("src");
    if (src) {
        let url = new URL(src);
        origin = url.origin;
    }
    return origin;
}
