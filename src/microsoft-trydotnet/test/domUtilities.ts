// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Configuration } from "../src";
import { JSDOM } from "jsdom";

export function buildSimpleIFrameDom(configuration: Configuration): JSDOM {
    let dom = new JSDOM(
        `<!DOCTYPE html>
        <html lang="en">
        <body>
            <iframe></iframe>
        </body>
        </html>`,
        {
            url: configuration.hostOrigin,
            runScripts: "dangerously"
        });

    return dom;
}


export function getEditorIFrame(dom: JSDOM): HTMLIFrameElement {
    let iframe = <HTMLIFrameElement>(dom.window.document.body.querySelector("iframe"));
    return iframe;
}