// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import MlsClient from "../../src/MlsClient";
import { uriThatECONNREFUSEDs } from "./constantUris";
import { failingFetcher } from "../Utility/failingFetcher";

export const getNetworkFailureClient = (uri?: URL) => {
    return new MlsClient(
        failingFetcher,
        uri || uriThatECONNREFUSEDs,
        () => undefined,
        null,
        uri || uriThatECONNREFUSEDs);
};
