// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { trimNewLines } from "../stringExtensions";

export function getCodeContainer(codeSource: HTMLElement): HTMLElement {
    let codeContainer = codeSource.parentElement;
    return codeContainer;
}

export function getCode(codeSource: HTMLElement): string {
    let code = codeSource.innerText;
    code = trimNewLines(code);
    return code;
}
