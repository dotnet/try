// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export function isNullOrUndefinedOrWhitespace(input: string): boolean {
    if (isNullOrUndefined(input)) {
        return true;
    }
    return input.replace(/\s/g, "").length < 1;
}

export function isNullOrUndefined(input: string): boolean {
    return input === undefined || input === null;
}

export function trimNewLines(input: string): string {
    return isNullOrUndefined(input) ? input : input.trim();
}

export function isString(input: any): input is string {
    return input.charAt !== undefined;
}

export function htmlEncode(input: string, doc: Document): string {
    let encoded = input;
    let source = doc;
    if (source) {
        let encoder = source.createElement("div");
        var text = source.createTextNode(input);
        encoder.appendChild(text);
        encoded = encoder.innerHTML;
    }

    return encoded;
}