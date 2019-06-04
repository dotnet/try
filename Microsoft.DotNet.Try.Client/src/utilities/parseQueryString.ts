// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

function parseQueryString(query: string): Map<string, string> {
    var map = new Map<string, string>();
    if (!query) {
        return map;
    }

    if (query[0] === "?") {
        query = query.slice(1);
    }

    return query.split("&").map(kv => kv.split("=")).reduce((_hash, pair) => {
        map.set(pair[0], pair[1]);
        return map;
    }, map);
}

export default parseQueryString;
