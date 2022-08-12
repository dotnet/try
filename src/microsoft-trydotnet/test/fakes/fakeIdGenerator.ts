// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IRequestIdGenerator } from "../../src/internals/requestIdGenerator";

export class FakeIdGenerator implements IRequestIdGenerator {
    getNewRequestId(): Promise<string> {
        return Promise.resolve("TestRun");
    }
}