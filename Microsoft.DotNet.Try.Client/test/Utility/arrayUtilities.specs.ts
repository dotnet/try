// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { groupBy, flatten } from "../../src/utilities/arrayUtilities";

describe("Array utilities", () => {
    describe("groupBy", () => {
        it("Should group an array into a map of arrays", () => {
            const groupByEvens = groupBy<number, boolean>(x => x % 2 == 0);
            groupByEvens([1, 2, 3, 4, 5]).get(true).should.deep.equal([2, 4]);
        });

        it("Should group array of objects", () => {
            const groupByA = groupBy<any, any>(x => x.a);
            groupByA([
                { a: 1, b: 2 },
                { a: 3, b: 1 },
                { a: 1, b: 1 }
            ]).get(1).should.deep.equal([
                { a: 1, b: 2 },
                { a: 1, b: 1 }
            ]);
        });
    });
    
    describe("flatten", () => {
        it("Should flatten array of arrays", () => {
            flatten([[1, 2], [3, 4]]).should.deep.equal([1, 2, 3, 4]);
        })
    })
});
