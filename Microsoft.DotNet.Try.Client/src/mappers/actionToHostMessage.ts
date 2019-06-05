// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as types from "../constants/ActionTypes";

import { AnyAction } from "redux";
import { AnyApiMessage, CREATE_PROJECT_RESPONSE, ApiMessage, CREATE_REGIONS_FROM_SOURCEFILES_RESPONSE, CREATE_OPERATION_ID_RESPONSE, MONACO_READY_EVENT, HOST_LISTENER_READY_EVENT, RUN_STARTED_EVENT, CODE_CHANGED_EVENT, RUN_COMPLETED_EVENT, CAN_MOVE_TO_NEXT_INSTRUMENTATION_RESPONSE, CAN_MOVE_TO_PREVIOUS_INSTRUMENTATION_RESPONSE, SERVICE_ERROR_RESPONSE, HOST_EDITOR_READY_EVENT, HOST_RUN_READY_EVENT } from "../constants/ApiMessageTypes";
function isNullOrUndefined(input: string): boolean {
    return (input === undefined || input === null);
}
export default function formatEventMessage(toLog: AnyAction, srcUri: string, editorId: string): AnyApiMessage {
    var result: AnyApiMessage = null;

    switch (toLog.type) {
        // system errors
        case types.LOAD_CODE_FAILURE:
            result = {
                type: "LoadCodeFailed"
            };
            break;

        case types.RUN_CODE_FAILURE:
        case types.SERVICE_ERROR:
            result = <ApiMessage><unknown>{
                type: SERVICE_ERROR_RESPONSE,
                statusCode: toLog.error.statusCode,
                message: toLog.error.message,
                requestId: toLog.error.requestId,
            };
            break;

        // API usages events
        case types.RUN_CLICKED:
            result = { type: RUN_STARTED_EVENT };
            break;

        case types.UPDATE_WORKSPACE_BUFFER:
            result = <ApiMessage><unknown>{
                type: CODE_CHANGED_EVENT,
                sourceCode: toLog.content,
                bufferId: toLog.bufferId
            };

            if (!isNullOrUndefined(editorId)) {
                result.editorId = editorId;
            } else if (!isNullOrUndefined(toLog.editorId)) {
                result.editorId = toLog.editorId;
            }
            break;

        case types.SET_ADDITIONAL_USINGS:
            result = {
                type: "SetadditionalUsings",
                SET_ADDITIONAL_USINGS: toLog.usings
            };
            break;

        case types.NOTIFY_HOST_LISTENER_READY:
            result = <ApiMessage><unknown>{
                type: HOST_LISTENER_READY_EVENT
            };

            if (!isNullOrUndefined(editorId)) {
                result.editorId = editorId;
            } else if (!isNullOrUndefined(toLog.editorId)) {
                result.editorId = toLog.editorId;
            }
            break;

        case types.NOTIFY_HOST_EDITOR_READY:
            result = <ApiMessage><unknown>{
                type: HOST_EDITOR_READY_EVENT
            };

            if (!isNullOrUndefined(editorId)) {
                result.editorId = editorId;
            } else if (!isNullOrUndefined(toLog.editorId)) {
                result.editorId = toLog.editorId;
            }
            break;

        case types.NOTIFY_HOST_RUN_READY:
            result = <ApiMessage><unknown>{
                type: HOST_RUN_READY_EVENT
            };

            if (!isNullOrUndefined(editorId)) {
                result.editorId = editorId;
            } else if (!isNullOrUndefined(toLog.editorId)) {
                result.editorId = toLog.editorId;
            }
            break;

        case types.WASMRUNNER_READY:
            result = <ApiMessage><unknown>{
                type: HOST_LISTENER_READY_EVENT
            };

            if (!isNullOrUndefined(editorId)) {
                result.editorId = editorId;
            } else if (!isNullOrUndefined(toLog.editorId)) {
                result.editorId = toLog.editorId;
            }
            break;

        case types.NOTIFY_MONACO_READY:
            result = {
                type: MONACO_READY_EVENT
            };
            break;

        case types.OUTPUT_UPDATED:
            result = {
                type: "OutputUpdated",
                output: toLog.output
            };
            break;

        case types.CAN_MOVE_NEXT:
            result = {
                type: CAN_MOVE_TO_NEXT_INSTRUMENTATION_RESPONSE,
                value: true
            };
            break;

        case types.CANNOT_MOVE_NEXT:
            result = {
                type: CAN_MOVE_TO_NEXT_INSTRUMENTATION_RESPONSE,
                value: false
            };
            break;

        case types.CAN_MOVE_PREV:
            result = {
                type: CAN_MOVE_TO_PREVIOUS_INSTRUMENTATION_RESPONSE,
                value: true
            };
            break;

        case types.CANNOT_MOVE_PREV:
            result = {
                type: CAN_MOVE_TO_PREVIOUS_INSTRUMENTATION_RESPONSE,
                value: false
            };
            break;

        // user events
        case types.RUN_CODE_SUCCESS:
            if (toLog.succeeded && !toLog.instrumentation) {
                result = {
                    type: RUN_COMPLETED_EVENT,
                    outcome: "Success",
                    output: toLog.output
                };
            } else if (toLog.succeeded && toLog.instrumentation) {
                result = {
                    type: RUN_COMPLETED_EVENT,
                    outcome: "Success",
                    instrumentation: true
                };
            } else {
                result = {
                    type: RUN_COMPLETED_EVENT,
                    outcome: "CompilationError",
                    output: toLog.output
                };
            }

            if (toLog.exception !== undefined && toLog.exception !== null) {
                result.exception = toLog.exception;
                result.outcome = "Exception";
            }
            break;

        case types.OPERATION_ID_GENERATED:
            result = {
                requestId: toLog.requestId,
                type: CREATE_OPERATION_ID_RESPONSE,
                operationId: toLog.operationId
            };
            break;

        case types.CREATE_PROJECT_FAILURE:
            result = <ApiMessage><unknown>{
                requestId: toLog.requestId,
                type: CREATE_PROJECT_RESPONSE,
                success: false,
                error: { ...toLog.error }
            };
            break;

        case types.CREATE_PROJECT_SUCCESS:
            result = <ApiMessage><unknown>{
                requestId: toLog.requestId,
                type: CREATE_PROJECT_RESPONSE,
                success: true,
                project: { ...toLog.project }
            };
            break;
        case types.CREATE_REGIONS_FROM_SOURCEFILES_FAILURE:
            result = <ApiMessage><unknown>{
                requestId: toLog.requestId,
                type: CREATE_REGIONS_FROM_SOURCEFILES_RESPONSE,
                success: false,
                error: { ...toLog.error }
            };
            break;
        case types.CREATE_REGIONS_FROM_SOURCEFILES_SUCCESS:
            result = <ApiMessage><unknown>{
                requestId: toLog.requestId,
                type: CREATE_REGIONS_FROM_SOURCEFILES_RESPONSE,
                success: true,
                regions: [...toLog.regions]
            };
            break;

        default:
            return result;
    }

    if (result) {
        if (toLog && toLog.requestId) {
            result.requestId = toLog.requestId;
        }

        if (srcUri) {
            result.messageOrigin = srcUri;
        }
    }

    return result;
}
