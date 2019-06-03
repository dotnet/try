// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import ensureIsString from "../../src/utilities/ensureIsString";
import { should } from "chai";

should();

describe("ensureIsString", () => {
    it("returns string without change", () => {
        var output = ensureIsString("foo");

        output.should.equal("foo");
    });

    it("converts object-wrapped string to string", () => {
        var output = ensureIsString(String("foo"));

        output.should.equal("foo");
    });

    it("converts number to string", () => {
        var output = ensureIsString(1);

        output.should.equal("1");
    });

    it("converts bool to string", () => {
        var output = ensureIsString(true);

        output.should.equal("true");
    });

    it("converts null to string", () => {
        var output = ensureIsString(null);

        output.should.equal("null");
    });

    it("converts objects to string", () => {
        var output = ensureIsString({});

        output.should.equal("{}");
    });
});
