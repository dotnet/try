// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";
import * as types from "../../src/constants/ActionTypes";

import { IDiagnostic, IInstrumentation, IVariableLocation } from "../../src/IState";
import getStore, { IObservableAppStore } from "../observableAppStore";
import IMlsClient, { IRunRequest, ICompileResponse } from "../../src/IMlsClient";
import actions from "../../src/actionCreators/actions";
import { cloneWorkspace } from "../../src/workspaces";
import { defaultWorkspace } from "../testResources";
import ServiceError from "../../src/ServiceError";
import { setWorkspace, updateWorkspaceBuffer } from "../../src/actionCreators/workspaceActionCreators";
import { getNetworkFailureClient } from "../mlsClient/getNetworkFailureClient";
import chaiExclude = require("chai-exclude");
import { IApplicationInsightsClient, NullAIClient } from "../../src/ApplicationInsights";
import { WasmCodeRunnerResponse, WasmCodeRunnerRequest } from "../../src/actionCreators/runActionCreators";
chai.use(require("deep-equal"));
chai.use(chaiExclude);

class ObservableAIClient implements IApplicationInsightsClient {

    public events: Array<{ name: string, properties?: { [key: string]: string; }, measurements?: { [key: string]: number; } }>;
    public dependencies: Array<{ method: string, absoluteUrl: string, pathName: string, totalTime: number, success: boolean, resultCode: number, operationId?: string }>;
    public exceptions: Array<{ exception: Error, handledAt?: string, properties?: { [key: string]: string; }, measurements?: { [key: string]: number; }, severityLevel?: AI.SeverityLevel }>;
    constructor() {
        this.events = [];
        this.dependencies = [];
        this.exceptions = [];
    }
    public trackEvent(name: string, properties?: { [key: string]: string; }, measurements?: { [key: string]: number; }): void {
        this.events.push({
            name,
            properties,
            measurements
        });
    }

    public trackDependency(method: string, absoluteUrl: string, pathName: string, totalTime: number, success: boolean, resultCode: number, operationId?: string): void {
        this.dependencies.push({
            method,
            absoluteUrl,
            pathName,
            totalTime,
            success,
            resultCode,
            operationId
        });
    }

    public trackException(exception: Error, handledAt?: string, properties?: { [key: string]: string; }, measurements?: { [key: string]: number; }, severityLevel?: AI.SeverityLevel): void {
        this.exceptions.push({
            exception,
            handledAt,
            properties,
            measurements,
            severityLevel
        });
    }
}

describe("RUN Action Creators", () => {
    var store: IObservableAppStore;

    beforeEach(() => {
        store = getStore();
    });

    it("should create an action to run", () => {
        const expectedAction = {
            type: types.RUN_CODE_REQUEST,
            requestId: "identifier"
        };

        actions.runRequest("identifier").should.deep.equal(expectedAction);
    });

    it("should create an action to process run success", () => {
        const expectedAction = {
            type: types.RUN_CODE_SUCCESS,
            exception: "some exception",
            executionStrategy: "Agent",
            output: ["some", "output"],
            succeeded: false,
            diagnostics: [] as IDiagnostic[],
            instrumentation: [] as IInstrumentation[],
            variableLocations: [] as IVariableLocation[]
        };

        actions
            .runSuccess({
                exception: "some exception",
                output: ["some", "output"],
                succeeded: false,
                diagnostics: [] as IDiagnostic[],
                instrumentation: [] as IInstrumentation[],
                variableLocations: [] as IVariableLocation[]
            })
            .should.deep.equal(expectedAction);
    });

    it("should create an action to process run success with requestId", () => {
        const expectedAction = {
            type: types.RUN_CODE_SUCCESS,
            exception: "some exception",
            executionStrategy: "Agent",
            output: ["some", "output"],
            succeeded: false,
            diagnostics: [] as IDiagnostic[],
            instrumentation: [] as IInstrumentation[],
            variableLocations: [] as IVariableLocation[],
            requestId: "TestRun"
        };

        actions
            .runSuccess({
                exception: "some exception",
                output: ["some", "output"],
                succeeded: false,
                diagnostics: [] as IDiagnostic[],
                instrumentation: [] as IInstrumentation[],
                variableLocations: [] as IVariableLocation[],
                requestId: "TestRun"
            })
            .should.deep.equal(expectedAction);
    });

    it("should create an action to process run failure", () => {
        var ex = new ServiceError(404, "Not Found");

        const expectedAction = {
            type: types.RUN_CODE_FAILURE,
            error: {
                statusCode: 404,
                message: "Not Found",
                requestId: ""
            }
        };

        actions.runFailure(ex).should.deep.equal(expectedAction);
    });

    it("should create an action to record a button click", () => {
        const expectedAction = {
            type: types.RUN_CLICKED
        };

        actions.runClicked().should.deep.equal(expectedAction);
    });

    it("creates RUN_CODE_REQUEST and RUN_CODE_SUCCESS", async () => {
        const workspace = cloneWorkspace(defaultWorkspace);
        var sourceCode = "Console.WriteLine(\"Hello, World\");";
        workspace.buffers = [{ id: "Program.cs", content: sourceCode, position: 0 }];
        workspace.files = [{ name: "Program.cs", text: sourceCode }];

        const expectedActions = [
            actions.setWorkspace(workspace),
            actions.runRequest("trydotnet.client_TestRun"),
            actions.runSuccess({
                output: ["Hello, World"],
                succeeded: true,
            })
        ];


        store.withDefaultClient();
        store.withAiClient(new NullAIClient());
        store.dispatch(actions.setWorkspace(workspace));
        await store.dispatch(actions.run("trydotnet.client_TestRun"));
        store.getActions().should.deep.equal(expectedActions);
    });

    it("creates RUN_CODE_REQUEST and RUN_CODE_FAILURE when fetching sourceCode from Uri fails", async () => {
        const expectedActions = [
            actions.runRequest("trydotnet.client_TestRun"),
            actions.runFailure(new Error("ECONNREFUSED"), "trydotnet.client_TestRun")
        ];


        let mockAiClient = new ObservableAIClient();

        store.withClient(getNetworkFailureClient());
        store.withAiClient(mockAiClient);

        store.configure([actions.setWorkspace(defaultWorkspace)]);

        await store.dispatch(actions.run("trydotnet.client_TestRun"));

        chai.expect(store.getActions()).excludingEvery("ex").to.deep.equal(expectedActions);
        chai.expect(mockAiClient.dependencies.length).to.eq(0);
        chai.expect(mockAiClient.events.length).to.eq(0);
        chai.expect(mockAiClient.exceptions.length).to.eq(1);
    });

    it("passes activeBufferId in the RunRequest", async () => {
        let actualRunRequest: any = { activeBufferId: undefined };
        const stubClient = {
            run: (args: IRunRequest): any => {
                actualRunRequest = args;
                return null;
            }
        };

        let mockAiClient = new ObservableAIClient();


        store.withClient(stubClient as IMlsClient);
        store.withAiClient(mockAiClient);
        store.configure([
            actions.setActiveBuffer("expected"),
            setWorkspace(defaultWorkspace)
        ]);

        await store.dispatch(actions.run());
        actualRunRequest.activeBufferId.should.deep.equal("expected");
    });


    it("passes additional parameters in the RunRequest", async () => {
        let actualRunRequest: any = { activeBufferId: undefined };
        let expectedRunRequest: any = { activeBufferId: "expected", workspace: { ...defaultWorkspace }, requestId: "testRun", runArgs: "done!" };
        const stubClient = {
            run: (args: IRunRequest): any => {
                actualRunRequest = args;
                return null;
            }
        };

        let mockAiClient = new ObservableAIClient();


        store.withClient(stubClient as IMlsClient);
        store.withAiClient(mockAiClient);
        store.configure([
            actions.setActiveBuffer("expected"),
            setWorkspace(defaultWorkspace)
        ]);

        await store.dispatch(actions.run("testRun", { runArgs: "done!" }));
        actualRunRequest.should.deep.equal(expectedRunRequest);
    });


    it("should create an action to compile", () => {
        const expectedAction = {
            type: types.COMPILE_CODE_REQUEST,
            requestId: "trydotnet.client_TestRun",
            workspaceVersion: 1
        };

        actions.compileRequest("trydotnet.client_TestRun", 1).should.deep.equal(expectedAction);
    });

    it("should include diagnostics in compile failure", () => {
        const response: ICompileResponse = {
            base64assembly: null,
            diagnostics: [{ end: 0, start: 0, message: "stuff", severity: 1 }],
            succeeded: false
        };

        const expectedAction = {
            type: types.COMPILE_CODE_FAILURE,
            diagnostics: response.diagnostics,
            workspaceVersion: 1
        };

        actions.compileFailure(response, 1).should.deep.equal(expectedAction);
    });

    it("should include diagnostics and requestId in compile failure", () => {
        const response: ICompileResponse = {
            base64assembly: null,
            diagnostics: [{ end: 0, start: 0, message: "stuff", severity: 1 }],
            succeeded: false,
            requestId: "trydotnet.client_TestRun",
        };

        const expectedAction = {
            type: types.COMPILE_CODE_FAILURE,
            diagnostics: response.diagnostics,
            requestId: "trydotnet.client_TestRun",
            workspaceVersion: 1
        };

        actions.compileFailure(response, 1).should.deep.equal(expectedAction);
    });

    it("should include assembly in compile success", () => {
        const response: ICompileResponse = {
            base64assembly: "foo",
            diagnostics: [],
            succeeded: true
        };

        const expectedAction = {
            type: types.COMPILE_CODE_SUCCESS,
            base64assembly: response.base64assembly,
            workspaceVersion: 1
        };

        actions.compileSuccess(response, 1).should.deep.equal(expectedAction);
    });

    it("sends telemetry if blazor fails", async () => {
        const stubClient = {
            compile: (_args: IRunRequest): any => {
                return {
                    succeeded: true
                };
            }
        };

        store.withClient(stubClient as IMlsClient);
        store.configure([
            setWorkspace(defaultWorkspace),
            actions.setWorkspaceType("blazor-console"),
            actions.configureBlazor(),
        ]);


        let mockAiClient = new ObservableAIClient();


        store.withAiClient(mockAiClient);

        await store.dispatch(actions.run());


        let response: WasmCodeRunnerResponse = {
            runnerException: "bad",
            codeRunnerVersion: "1.0.0"
        };

        let blazorResult = {
            codeRunnerVersion: "something",
            data: response
        };

        store.getState().blazor.callback(blazorResult);
        chai.expect(mockAiClient.dependencies.length).to.eq(0);
        chai.expect(mockAiClient.events.length).to.eq(1);
        chai.expect(mockAiClient.exceptions.length).to.eq(0);
    });

    it("dispatches RUN_CODE_FAILURE if blazor compile fails", async () => {
        const stubClient = {
            compile: (_args: IRunRequest): any => {
                throw new Error("compiler failure!!!");

            }
        };

        store.withClient(stubClient as IMlsClient);
        store.configure([
            setWorkspace(defaultWorkspace),
            actions.setWorkspaceType("blazor-console"),
            actions.configureBlazor(),
        ]);

        await store.dispatch(actions.run());

        let runFailure = store.getActions().find(a => a.type === types.RUN_CODE_FAILURE);
        // tslint:disable-next-line:no-unused-expression-chai
        runFailure.should.not.be.null;
    });

    it("dispatches RUN_CODE_SUCCESS if blazor compile fails and contains diagnostics in output", async () => {
        const stubClient = {
            compile: (_args: IRunRequest): any => {
                return {
                    succeeded: false,
                    diagnostics: [{ message: "not compiling code with typos" }]
                };
            }
        };

        store.withClient(stubClient as IMlsClient);
        store.configure([
            setWorkspace(defaultWorkspace),
            actions.setWorkspaceType("blazor-console"),
            actions.configureBlazor(),
        ]);

        await store.dispatch(actions.run());

        let runSuccess = store.getActions().find(a => a.type === types.RUN_CODE_SUCCESS);
        // tslint:disable-next-line:no-unused-expression-chai
        runSuccess.should.not.be.null;
        (<any>runSuccess).output.should.deep.equal(["not compiling code with typos"]);
    });

    it("sends telemetry if blazor succeeds", async () => {
        const stubClient = {
            compile: (_args: IRunRequest): any => {
                return {
                    succeeded: true
                };
            }
        };

        store.withClient(stubClient as IMlsClient);
        store.configure([
            setWorkspace(defaultWorkspace),
            actions.setWorkspaceType("blazor-console"),
            actions.configureBlazor(),
        ]);
        let mockAiClient = new ObservableAIClient();

        store.withAiClient(mockAiClient);

        await store.dispatch(actions.run());

        let response: WasmCodeRunnerResponse = {
            succeeded: true,
            output: ["good"],
            codeRunnerVersion: "1.0.0"
        };

        let blazorResult = {
            codeRunnerVersion: "something",
            data: response
        };

        store.getState().blazor.callback(blazorResult);
        chai.expect(mockAiClient.dependencies.length).to.eq(0);
        let event = mockAiClient.events.find(e => e.name === "Blazor.Success");
        // tslint:disable-next-line:no-unused-expression-chai
        chai.expect(event).not.to.be.undefined;
        chai.expect(mockAiClient.exceptions.length).to.eq(0);
    });

    it("runWithBlazor does not compile if the workspace hasn't changed", async () => {

        let compileCount = 0;

        const stubClient = {
            compile: (_args: IRunRequest): any => {
                compileCount += 1;
                return {
                    succeeded: true
                };
            }
        };

        store.withClient(stubClient as IMlsClient);
        store.configure([
            setWorkspace(defaultWorkspace),
            actions.setWorkspaceType("blazor-console"),
            actions.configureBlazor(),
        ]);

        chai.expect(compileCount).to.eq(0);
        await store.dispatch(actions.run());
        chai.expect(compileCount).to.eq(1);

        await store.dispatch(actions.run());
        chai.expect(compileCount).to.eq(1);
    });

    it("runWithBlazor compiles if the workspace has changed", async () => {

        let compileCount = 0;

        const stubClient = {
            compile: (_args: IRunRequest): any => {
                compileCount += 1;
                return {
                    succeeded: true
                };
            }
        };

        store.withClient(stubClient as IMlsClient);
        store.configure([
            setWorkspace(defaultWorkspace),
            actions.setWorkspaceType("blazor-console"),
            actions.configureBlazor(),
        ]);

        chai.expect(compileCount).to.eq(0);
        await store.dispatch(actions.run());
        chai.expect(compileCount).to.eq(1);

        store.dispatch(updateWorkspaceBuffer("foo", "Program.cs"));

        await store.dispatch(actions.run());
        chai.expect(compileCount).to.eq(2);
    });

    it("runWithBlazor passes additional parameters in the RunRequest", async () => {
        const stubClient = {
            compile: (args: IRunRequest): any => {
                return {
                    requestId: args.requestId,
                    succeeded: true
                };
            }
        };

        store.withClient(stubClient as IMlsClient);
        store.configure([
            setWorkspace(defaultWorkspace),
            actions.setWorkspaceType("blazor-console"),
            actions.configureBlazor(),
        ]);
        let mockAiClient = new ObservableAIClient();

        store.withAiClient(mockAiClient);

        await store.dispatch(actions.run("testRun", { runArgs: "done!" }));

        let response: WasmCodeRunnerResponse = {
            succeeded: true,
            output: ["good"],
            codeRunnerVersion: "1.0.0"
        };

        let blazorResult = {
            codeRunnerVersion: "something",
            data: response
        };

        store.getState().blazor.callback(blazorResult);
        chai.expect(mockAiClient.dependencies.length).to.eq(0);
        let event = mockAiClient.events.find(e => e.name === "Blazor.Success");
        let wasmRunRequestAction = store.getActions().find(a => a.type === types.SEND_BLAZOR_MESSAGE);

        // tslint:disable-next-line:no-unused-expression-chai
        chai.expect(wasmRunRequestAction).not.to.be.undefined;
        // tslint:disable-next-line:no-unused-expression-chai
        chai.expect(event).not.to.be.undefined;
        chai.expect(mockAiClient.exceptions.length).to.eq(0);

        let request: WasmCodeRunnerRequest = <WasmCodeRunnerRequest>((<any>wasmRunRequestAction).payload);
        request["runArgs"].should.be.equal("done!");
    });
});
