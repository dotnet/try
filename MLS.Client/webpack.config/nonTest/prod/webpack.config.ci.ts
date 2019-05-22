// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import merge = require("webpack-merge");
import common from "../webpack.config";
export { APP_DIR } from "../webpack.config";

let config = merge.smart(common, {
    devtool: "source-map"
}, 
 {
    module: {
        rules: [
            {
                test: /\.tsx?$/,
                use: [{ loader: "babel-loader?cacheDirectory" }, {
                    loader: "ts-loader", options: {
                        transpileOnly: false,
                        experimentalWatchApi: true,
                    }
                }],
                exclude: /node_modules/
            }
        ]
    },
    resolve: {
        extensions: [".tsx", ".ts", ".js"]
    }});

export default config;
