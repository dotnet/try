// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import mlsClientSpecs from "./mlsClient.specs";

import MlsClientSimulator from "./mlsClient.simulator";
import {suite} from "mocha-typescript";

suite("MlsClient Simulator", function () {
    mlsClientSpecs(
        async () => Promise.resolve(new MlsClientSimulator()),
        "simulator");
});
