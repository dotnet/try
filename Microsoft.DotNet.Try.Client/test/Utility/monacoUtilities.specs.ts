// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import createCodeLens from "../../src/utilities/monacoUtilities";

describe("monacoUtilities", () => {
    describe("createCodeLens", () => {
        it("should create a code lens symbol from parameters", () => {
            createCodeLens("expectedText", 1, 0, "expectedCommandId")
                .should.deep.equal(
                    {
                        range: {
                            startLineNumber: 1,
                            startColumn: 1,
                            endLineNumber: 2,
                            endColumn: 1
                        },
                        command: {
                            title: "expectedText",
                            id: "expectedCommandId"
                        }
                    }
                );
        });
    });
});
