// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import appendQuery from "../../src/utilities/appendQuery";
import { should } from "chai";

should();

describe("appendQuery", () => {
    it("delimits the new name-value pair with '?' when no query string is present", () => {
        var uri = appendQuery("http://example.com/home", "this", "that");

        uri.should.equal("http://example.com/home?this=that");
    });

    it("delimits the new name-value pair with '&' when a query string is present", () => {
        var uri = appendQuery(
            "http://example.com/home?this=that",
            "also",
            "the-other-thing"
        );

        uri.should.equal("http://example.com/home?this=that&also=the-other-thing");
    });
});
