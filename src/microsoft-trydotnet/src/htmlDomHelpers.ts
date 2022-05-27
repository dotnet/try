// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Configuration } from "./configuration";
import { generateEditorUrl } from "./internals/urlHelpers";

export function configureEmbeddableEditorIFrame(iframe: HTMLIFrameElement, configuration: Configuration): HTMLIFrameElement {
    if (configuration) {
        let src = iframe.getAttribute("src")
        if (!src) {
            const url = generateEditorUrl(configuration);
            iframe.setAttribute("src", url);
        }
    }
    return iframe;
}


export function configureEmbeddableEditorIFrameWithPackage(iframe: HTMLIFrameElement, configuration: Configuration, packageName: string): HTMLIFrameElement {
    if (configuration) {
        let src = iframe.getAttribute("src")
        if (!src) {
            const url = generateEditorUrl(configuration, packageName);
            iframe.setAttribute("src", url);
        }
    }
    return iframe;
}