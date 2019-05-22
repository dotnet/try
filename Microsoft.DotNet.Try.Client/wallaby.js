// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const processCss = function (file, done) {
    done(JSON.stringify({}));
};

module.exports = function (wallaby) {
    return {
        env: {
            type: "node"
        },
        files: [
            {
                pattern: "node_modules/react/dist/react-with-addons.js",
                instrument: false
            },
            "src/**/*.ts*",
            "src/**/*.css",
            "test/**/*.ts*",
            "!test/**/*.specs.ts*"
        ],
        tests: ["test/**/*.specs.ts*"],
        preprocessors: {
            'src/**/*.css': processCss
        },
        debug: true,
        compilers: {
            '**/*.ts?(x)': wallaby.compilers.typeScript({
                typescript: require('typescript'),
                module: 'commonjs',
            })
        },
        testFramework:'mocha'
    };
};
