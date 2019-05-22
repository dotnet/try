// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

function ensureIsString(input: any) {
    if (typeof input === "string") {
        return input;
    }

    return JSON.stringify(input);
}

export default ensureIsString;
