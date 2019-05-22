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

export function buildMultiIFrameDom(configuration: Configuration): JSDOM {
    let dom = new JSDOM(
        `<!DOCTYPE html>
        <html lang="en">
        <body>
            <iframe data-trydotnet-editor-id="a"></iframe>
            <iframe data-trydotnet-editor-id="b"></iframe>
            <iframe data-trydotnet-editor-id="c"></iframe>
        </body>
        </html>`,
        {
            url: configuration.hostOrigin,
            runScripts: "dangerously"
        });

    return dom;
}

export function buildDoubleIFrameDom(configuration: Configuration): JSDOM {
    let dom = new JSDOM(
        `<!DOCTYPE html>
        <html lang="en">
        <body>
            <iframe data-trydotnet-editor-id="a"></iframe>
            <iframe data-trydotnet-editor-id="b"></iframe>
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

export function getEditorIFrames(dom: JSDOM): HTMLIFrameElement[] {
    let nodes = dom.window.document.body.querySelectorAll("iframe");
    let iframes: HTMLIFrameElement[] = [];
    nodes.forEach(iframe => {
        iframes.push(iframe);
    });  
    return iframes;
} 