// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import merge = require("webpack-merge");
import common from "../webpack.config";
export { APP_DIR } from "../webpack.config";

let config = merge(common, {
    devtool: "inline-source-map"
});

export default config;
