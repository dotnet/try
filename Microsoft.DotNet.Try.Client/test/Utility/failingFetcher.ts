// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { uriThatENOTFOUNDs, uriThatECONNREFUSEDs, uriThat404s } from "../mlsClient/constantUris";
import { ICanFetch } from "../../src/MlsClient";


export const failingFetcher: ICanFetch = async (uri: string) => {

    await Promise.resolve(0);
    
    var url = new URL(uri);

    switch (true) {
        case url.href.startsWith(uriThatENOTFOUNDs.href):
            throw new Error("ENOTFOUND");
        
        case url.href.startsWith(uriThatECONNREFUSEDs.href):
            throw new Error("ECONNREFUSED");
        
        case url.href.startsWith(uriThat404s.href):
            return { 
                ok: false,
                status: 404,
                statusText: "Not Found" 
            };
    }
};
