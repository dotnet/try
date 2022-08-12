// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

module.exports = function (wallaby) {
    return {
        env: {
            type: "node"
        },
        files: [
            "src/**/*.ts*",
            { pattern: "tests/**/*.json", instrument: false },
            { pattern: "tests/**/*.ts*", instrument: true },
            "!tests/**/*.tests.ts*",
        ],
        tests: ["tests/**/*.tests.ts*"],
        compilers: {
            '**/*.ts?(x)': wallaby.compilers.typeScript({})
        },
        testFramework: 'mocha',
        debug: true,
        setup: function (wallaby) {
            var mocha = wallaby.testFramework;
            mocha.timeout(10000);
        }
    };
};
