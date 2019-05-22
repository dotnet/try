// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as types from "../constants/ActionTypes";
import { Action } from "../constants/ActionTypes";

export function error(errorType: string = "general", reason?: string): Action {
    return {
        type: types.REPORT_ERROR,
        errorType,
        reason
    };
}
