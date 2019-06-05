// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import IState from "../IState";
import { combineReducers } from "redux";
import config from "./configReducer";
import monaco from "./monacoReducer";
import run from "./runReducer";
import compile from "./compileReducer";
import ui from "./uiReducer";
import workspace from "./workspaceReducer";
import workspaceInfo from "./workspaceInfoReducer";
import wasmRunner from "./wasmRunnerReducer";

export default combineReducers<IState>({
    config,
    monaco,
    run,
    ui,
    workspace,
    workspaceInfo,
    compile,
    wasmRunner
});
