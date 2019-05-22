// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { encodeWorkspace, decodeWorkspace } from "../../src/workspaces";
import { should } from "chai";

describe("workspace", () => {
    describe("decoding", () => {
        it("should throw when using invalid source", () => {
            const encoded = "nonsense";
            should().throw(() => {
                decodeWorkspace(encoded);
            });
        });
    });
    describe("encoding", () => {
        it("should not throw when using null source", () => {
            should().not.throw(() => {
                encodeWorkspace(null);
            });
        });
    });
});
