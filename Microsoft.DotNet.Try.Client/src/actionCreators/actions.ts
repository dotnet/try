// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as config from "./configActionCreators";
import * as control from "./controlActionCreators";
import * as notification from "./notificationActionCreators";
import * as run from "./runActionCreators";
import * as source from "./sourceCodeActionCreators";
import * as workspace from "./workspaceActionCreators";
import * as error from "./errorActionCreators";
import * as ui from "./uiActionCreators";
import * as instrumentation from "./instrumentationActionCreators";
import * as requestId from "./operationIdActionCreators";
import * as project from "./projectActionCreators";
import * as sourceFiles from "./sourceFileActionCreators";

const actions = {
    ...config,
    ...control,
    ...notification,
    ...run,
    ...source,
    ...workspace,
    ...error,
    ...ui,
    ...instrumentation,
    ...requestId,
    ...project,
    ...sourceFiles
};

export default actions;
