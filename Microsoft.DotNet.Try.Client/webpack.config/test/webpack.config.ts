// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as path from "path";
import common from "../webpack.config";
import { APP_DIR } from "../webpack.config";
import merge = require("webpack-merge");
export { APP_DIR } from "../webpack.config";

let config = merge(common, {
    mode: "development",
    devtool: "inline-source-map",
    entry: [path.resolve(APP_DIR, "components", "index.tsx")],
    output: {
        // use absolute paths in sourcemaps (important for debugging via IDE)
        devtoolModuleFilenameTemplate: "[absolute-resource-path]",
        devtoolFallbackModuleFilenameTemplate: "[absolute-resource-path]?[hash]",
        filename: undefined,
        sourceMapFilename: undefined,
        path: path.resolve(__dirname, "obj"),
        publicPath: undefined
    },
    module: {
        rules: [
            {
                test: /\.app.css$/,
                use: [
                    {
                        loader: "typings-for-css-modules-loader?modules",
                        options: {
                            sourceMap: true,
                            importLoaders: 3,
                            modules: true,
                            namedExport: true,
                            camelCase: true,
                            localIdentName: "[name]-[local]-[hash:5]"
                        }
                    }
                ]
            },
        ]
    },
    target: "node",
    resolve: {
        alias: {
            "monaco-editor": path.resolve(APP_DIR, "../", "test", "fakeMonacoStuff.ts"),
        }
    }
});

export default config;
