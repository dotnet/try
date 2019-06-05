// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import action from "../../src/actionCreators/actions";
import map from "../../src/mappers/actionToHostMessage";
import { should } from "chai";
import ServiceError from "../../src/ServiceError";
import { IInstrumentation } from "../../src/IState";
import { defaultGistProject } from "../testResources";
import { CREATE_OPERATION_ID_RESPONSE, CREATE_PROJECT_RESPONSE, CREATE_REGIONS_FROM_SOURCEFILES_RESPONSE } from "../../src/constants/ApiMessageTypes";

describe("actionToHostMessage mapper", function () {

    // default case handling
    it("returns empty for not supported messages with requestId", function () {
        var result = map(action.runRequest("testRun"), "https://try.dot.net", "editorId");
        should().not.exist(result);
    });

    it("returns empty for not supported messages", function () {
        var result = map(action.loadCodeRequest(""), "https://try.dot.net", "editorId");
        should().not.exist(result);
    });

    // API usages events
    it("maps RUN_CLICKED to RunStarted message", function () {
        var expectedResult = {
            type: "RunStarted",
            messageOrigin: "https://try.dot.net"
        };

        var result = map(action.runClicked(), "https://try.dot.net", "editorId");
        result.should.deep.equal(expectedResult);
    });

    it("maps UPDATE_WORKSPACE_BUFFER to CodeModified message", function () {
        var expectedResult = {
            sourceCode: "foo()",
            editorId: "editorId",
            type: "CodeModified",
            bufferId: "Program.cs",
            messageOrigin: "https://try.dot.net"
        };

        var result = map(action.updateWorkspaceBuffer("foo()", "Program.cs"), "https://try.dot.net", "editorId");
        result.should.deep.equal(expectedResult);
    });

    it("maps NOTIFY_HOST_LISTENER_READY to HostListenerReady message", function () {
        var expectedResult = {
            type: "HostListenerReady",
            messageOrigin: "https://try.dot.net",
            editorId: "listenerA"
        };

        var result = map(action.hostListenerReady("listenerA"), "https://try.dot.net", null);
        result.should.deep.equal(expectedResult);
    });

    it("maps NOTIFY_HOST_RUN_READY to HostRunReady message", function () {
        var expectedResult = {
            type: "HostRunReady",
            messageOrigin: "https://try.dot.net",
            editorId: "listenerA"
        };

        var result = map(action.hostRunReady("listenerA"), "https://try.dot.net", null);
        result.should.deep.equal(expectedResult);
    });

    it("maps NOTIFY_HOST_EDITOR_READY to HostEditorReady message", function () {
        var expectedResult = {
            type: "HostEditorReady",
            messageOrigin: "https://try.dot.net",
            editorId: "listenerA"
        };

        var result = map(action.hostEditorReady("listenerA"), "https://try.dot.net", null);
        result.should.deep.equal(expectedResult);
    });
    
    it("maps WASMRUNNER_READY to HostListenerReady message", function () {
        var expectedResult = {
            type: "HostListenerReady",
            messageOrigin: "https://try.dot.net",
            editorId: "listenerA"
        };

        var result = map(action.wasmRunnerReady("listenerA"), "https://try.dot.net", null);
        result.should.deep.equal(expectedResult);
    });

    it("maps NOTIFY_MONACO_READY to MonacoEditorReady message", function () {
        var expectedResult = {
            type: "MonacoEditorReady",
            messageOrigin: "https://try.dot.net"
        };

        var result = map(action.notifyMonacoReady({ focus: () => { }, layout: () => { } }), "https://try.dot.net", "editorId");
        result.should.deep.equal(expectedResult);
    });

    // user events
    it("maps RUN_CODE_SUCCESS to RunCompleted message with outcome: 'Success'", function () {
        var expectedResult = {
            type: "RunCompleted",
            outcome: "Success",
            output: ["some", "lines"],
            messageOrigin: "https://try.dot.net"
        };

        var result = map(action.runSuccess({ output: ["some", "lines"], succeeded: true }), "https://try.dot.net", "editorId");
        result.should.deep.equal(expectedResult);
    });

    it("maps RUN_CODE_SUCCESS to RunCompleted message with outcome: 'CompilationError'", function () {
        var expectedResult = {
            type: "RunCompleted",
            outcome: "CompilationError",
            output: ["some", "lines"],
            messageOrigin: "https://try.dot.net"
        };

        var result = map(action.runSuccess({ output: ["some", "lines"], succeeded: false }), "https://try.dot.net", "editorId");
        result.should.deep.equal(expectedResult);
    });

    it("maps RUN_CODE_SUCCESS to RunCompleted message with outcome: 'Exception'", function () {
        var expectedResult = {
            type: "RunCompleted",
            exception: "some exception with stack trace",
            outcome: "Exception",
            output: ["some", "lines"],
            messageOrigin: "https://try.dot.net"
        };

        var result = map(action.runSuccess({ exception: "some exception with stack trace", output: ["some", "lines"], succeeded: false }), "https://try.dot.net", "editorId");
        result.should.deep.equal(expectedResult);
    });

    it("maps RUN_CODE_SUCCESS with instrumentation to RunCompleted with instrumentation and without output", () => {
        map(action.runSuccess({
            output: ["something"],
            succeeded: true,
            instrumentation: [] as IInstrumentation[]
        }), "https://try.dot.net", "editorId").should.deep.equal({
            type: "RunCompleted",
            outcome: "Success",
            instrumentation: true,
            messageOrigin: "https://try.dot.net"
        });
    });

    // system errors
    it("maps LOAD_CODE_FAILURE to LoadCodeFailed message", function () {
        var expectedResult = {
            type: "LoadCodeFailed",
            messageOrigin: "https://try.dot.net"
        };

        var result = map(action.loadCodeFailure(new Error("900")), "https://try.dot.net", "editorId");
        result.should.deep.equal(expectedResult);
    });

    it("maps RUN_CODE_FAILURE to ServiceError message with outcome: 'Exception'", function () {
        var expectedResult = {
            type: "ServiceError",
            statusCode: 503,
            message: "Service Unavailable",
            requestId: "123456",
            messageOrigin: "https://try.dot.net"
        };

        var result = map(action.runFailure(new ServiceError(503, "Service Unavailable", "123456")), "https://try.dot.net", "editorId");
        result.should.deep.equal(expectedResult);
    });

    // instrumentation
    it("maps OUTPUT_UPDATED to OutpuUpdated with output", () => {
        map(action.outputUpdated(["expected"]), "https://try.dot.net", "editorId")
            .should.deep.equal({
                type: "OutputUpdated",
                output: ["expected"],
                messageOrigin: "https://try.dot.net"
            });
    });
    it("maps CANNOT_MOVE_NEXT to CanMoveNext with value false", () => {
        map(action.cannotMoveNext(), "https://try.dot.net", "editorId")
            .should.deep.equal({
                type: "CanMoveNext",
                value: false,
                messageOrigin: "https://try.dot.net"
            });
    });

    it("maps CAN_MOVE_NEXT to CanMoveNext with value true", () => {
        map(action.canMoveNext(), "https://try.dot.net", "editorId")
            .should.deep.equal({
                type: "CanMoveNext",
                value: true,
                messageOrigin: "https://try.dot.net"
            });
    });

    it("maps CANNOT_MOVE_PREV to CanMovePrev with value false", () => {
        map(action.cannotMovePrev(), "https://try.dot.net", "editorId")
            .should.deep.equal({
                type: "CanMovePrev",
                value: false,
                messageOrigin: "https://try.dot.net"
            });
    });

    it("maps CAN_MOVE_PREV to CanMovePrev with value true", () => {
        map(action.canMovePrev(), "https://try.dot.net", "editorId")
            .should.deep.equal({
                type: "CanMovePrev",
                value: true,
                messageOrigin: "https://try.dot.net"
            });
    });

    it("maps OPERATION_ID_GENERATED to OperationIdGenerated", () => {
        let message = map(action.generateOperationId("Test_Run"), "https://try.dot.net", "editorId");
        message.type.should.equal(CREATE_OPERATION_ID_RESPONSE);
        message.requestId.should.equal("Test_Run");
        message.operationId.should.not.equal(null);
    });

    it("maps CREATE_PROJECT_FAILURE to createprojectresponse with success set to false", () => {
        let message = map(action.projectCreationFailure(new Error("general"), "Test_Run"), "https://try.dot.net", "editorId");
        message.type.should.equal(CREATE_PROJECT_RESPONSE);
        message.requestId.should.equal("Test_Run");
        message.success.should.be.equal(false);
    });

    it("maps CREATE_PROJECT_SUCCESS to createprojectresponse with success set to true", () => {
        let message = map(action.projectCreationSuccess({ requestId: "Test_Run", project: { ...defaultGistProject } }), "https://try.dot.net", "editorId");
        message.type.should.equal(CREATE_PROJECT_RESPONSE);
        message.requestId.should.equal("Test_Run");
        message.success.should.be.equal(true);
        message.project.should.not.be.equal(null);
    });

    it("maps GENERATE_REGIONS_FROM_FILES_FAILURE to generateregionfromfilesresponse with success set to false", () => {
        let message = map(action.createRegionsFromProjectFilesFailure(new Error("general"), "Test_Run"), "https://try.dot.net", "editorId");
        message.type.should.equal(CREATE_REGIONS_FROM_SOURCEFILES_RESPONSE);
        message.requestId.should.equal("Test_Run");
        message.success.should.be.equal(false);
    });

    it("maps GENERATE_REGIONS_FROM_FILES_SUCCESS to generateregionfromfilesresponse with success set to true", () => {
        let message = map(action.createRegionsFromProjectFilesSuccess({ requestId: "Test_Run", regions: [{ id: "code.cs", content: "code here" }] }), "https://try.dot.net", "editorId");
        message.type.should.equal(CREATE_REGIONS_FROM_SOURCEFILES_RESPONSE);
        message.requestId.should.equal("Test_Run");
        message.success.should.be.equal(true);
        message.regions[0].id.should.be.equal("code.cs");
        message.regions[0].content.should.be.equal("code here");
    });
});
