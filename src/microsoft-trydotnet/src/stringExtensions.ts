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
