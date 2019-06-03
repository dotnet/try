// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as types from "../constants/ActionTypes";
import { Action } from "../constants/ActionTypes";
import { newOperationId } from "../utilities/requestIdGenerator";

export function generateOperationId(requestId:string): Action {
    let operationId = newOperationId();
    return {
        type: types.OPERATION_ID_GENERATED,
        operationId: operationId,
        requestId: requestId
    };
}
