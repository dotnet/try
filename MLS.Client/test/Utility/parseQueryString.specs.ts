// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import parseQueryString from "../../src/utilities/parseQueryString";
import { should } from "chai";

should();

describe("parseQueryString", () => {
    it("can parse a query string with a leading `?`", () => {
        var query = parseQueryString("?this=that");

        query.get("this").should.equal("that");
    });

    it("can parse a query string without a leading `?`", () => {
        var query = parseQueryString("this=that");

        query.get("this").should.equal("that");
    });

    it("can parse multiple values", () => {
        var query = parseQueryString("?this=that&the-other=thing");

        query.get("this").should.equal("that");
        query.get("the-other").should.equal("thing");
    });

    it("can parse an empty query string", () => {
        var query = parseQueryString("");

        should().not.exist(query.get("this"));
    });

    it("can parse a query string consisting only of a `?`", () => {
        var query = parseQueryString("?");

        should().not.exist(query.get("this"));
    });

    it("does not decode values", () => {
        var query = parseQueryString("?uri=https%3A%2F%2Fmicrosoft.com");

        query.get("uri").should.equal("https%3A%2F%2Fmicrosoft.com");
    });
});
