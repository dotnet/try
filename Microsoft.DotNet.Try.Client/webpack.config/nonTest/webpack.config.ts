// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as path from "path";
import common from "../webpack.config";
import merge = require("webpack-merge");
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const MonacoWebpackPlugin = require("monaco-editor-webpack-plugin");
export { APP_DIR } from "../webpack.config";
import { APP_DIR } from "../webpack.config";

let config = merge(common, {
    entry: ["babel-polyfill", path.resolve(APP_DIR, "components", "index.tsx")],
    externals: ["enzyme"],
    mode: "development",
    output: {
        filename: "bundle.js",
        sourceMapFilename: "bundle.js.map",
        path: path.resolve(APP_DIR, "../", "../", "MLS.Agent", "wwwroot", "client"),
        publicPath: "/client/"
    },
    module: {
        rules: [
            {
                test: /\.app.css$/,
                use: [
                    { loader: MiniCssExtractPlugin.loader },
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
            }
        ]
    },
    plugins: [
        new MonacoWebpackPlugin({ languages: ["csharp"] }),
        new MiniCssExtractPlugin({
            filename: "bundle.css",
            chunkFilename: "chunk.css"
        })
    ]
});

export default config;
