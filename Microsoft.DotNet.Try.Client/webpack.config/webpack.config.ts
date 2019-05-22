// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as path from "path";
import * as webpack from "webpack";
export const APP_DIR = path.resolve(__dirname, "../", "src");
const MiniCssExtractPlugin = require("mini-css-extract-plugin");

const config: webpack.Configuration = {
    module: {
        rules: [
            {
                test: /\.tsx?$/,
                use: [{ loader: "babel-loader?cacheDirectory" }, {
                    loader: "ts-loader", options: {
                        transpileOnly: true,
                        experimentalWatchApi: true,
                    }
                }],
                exclude: /node_modules/
            },
            {
                test: /\.(js|jsx)$/,
                exclude: /node_modules/,
                use: [{ loader: "babel-loader?cacheDirectory" }]
            },
            {
                test: /^((?!\.app).)*.css$/,
                use: [
                    { loader: MiniCssExtractPlugin.loader },
                    { loader: "css-loader" }
                ]
            },
        ]
    },
    resolve: {
        extensions: [".tsx", ".ts", ".js"]
    }
};

export default config;
