import * as webpack from "webpack";
import common from "../webpack.config";
import merge = require("webpack-merge");
const WebpackMonitor = require("webpack-monitor");
const SpeedMeasurePlugin = require("speed-measure-webpack-plugin");
export { APP_DIR } from "../webpack.config";

const config: webpack.Configuration = 
    new SpeedMeasurePlugin().wrap(merge(common, {
            plugins: [
                new WebpackMonitor({
                    capture: true,
                    launch: true,
                })
            ]
        }));

export default config;
