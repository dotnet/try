// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export default function appendQuery(uri: string, name: string, value: string) {
    let delimiter = /[?&]/.test(uri) ? "&" : "?";

    return `${uri}${delimiter}${name}=${value}`;
}
