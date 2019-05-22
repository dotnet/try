// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { isNullOrUndefinedOrWhitespace } from "../stringExtensions";
import { tryDotNetVisibilityModifiers } from "./types";

export function getTrydotnetSessionId(
    element: HTMLElement,
    defualtSessionId: string = "default"
): string {
    let sessionId = element.dataset.trydotnetSessionId;
    if (!sessionId) {
        sessionId = defualtSessionId;
    }
    return sessionId;
}

export function getTrydotnetEditorId(element: HTMLElement): string {
    let editorId = element.dataset.trydotnetEditorId;
    return editorId;
}

export function getVisibility(element: HTMLElement) {
    let visibility = element.dataset.trydotnetVisibility;
    if(isNullOrUndefinedOrWhitespace(visibility)){
        visibility = tryDotNetVisibilityModifiers[tryDotNetVisibilityModifiers.visible]
    }
    return visibility;
}
