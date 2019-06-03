// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ICanFetch, Request } from "../MlsClient";
const fetch = require("cross-fetch");

const fetcher: ICanFetch = async (uri: string, request: Request) => {
    
    if (uri.startsWith("/")) {
        uri = "http://localhost" + uri;
    }

    return Promise.race([
        // timeout, 
        fetch(uri, request) as any
    ]);

};

export default fetcher; 
