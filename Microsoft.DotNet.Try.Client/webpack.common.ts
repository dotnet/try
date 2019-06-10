// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// good article: https://medium.com/webpack/unambiguous-webpack-config-with-typescript-8519def2cac7
import * as path from "path";
import * as webpack from "webpack";

var APP_DIR = path.resolve(__dirname, "./src");

const config: webpack.Configuration = {
    entry: [path.resolve(APP_DIR, "/components/index.tsx")],
    //externals: ["enzyme"],
    output: {
        filename: "bundle.js",
        sourceMapFilename: "bundle.js.map",
        path: path.resolve(__dirname, "../", "../", "MLS.Agent", "wwwroot", "client"),
        publicPath: "/client/"
    },
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
            }
        ]
    },
    resolve: {
        extensions: [".tsx", ".ts", ".js"]
    }
};

export default config;
