// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as types from "../../src/constants/ActionTypes";

import { IRunState, IDiagnostic, IInstrumentation, IVariableLocation } from "../../src/IState";
import reducer from "../../src/reducers/runReducer";
import actions from "../../src/actionCreators/actions";
import { Action } from "../../src/constants/ActionTypes";

const initialState: IRunState = {
    exception: undefined,
    output: undefined,
    succeeded: undefined,
    diagnostics: undefined,
    instrumentation: undefined,
    variableLocations: undefined,
    currentInstrumentationStep: undefined
};

describe("run Reducer", () => {
    it("should return the initial state", () => {
        reducer(undefined, { type: undefined }).should.deep.equal(initialState);

    });

    it("should handle RUN_CODE_SUCCESS and update state", () => {
        const originalState = { salt: 1 };
        const action: any = {
            type: types.RUN_CODE_SUCCESS,
            exception: "e",
            output: ["a", "b"],
            succeeded: true,
            diagnostics: [{ start: 1, end: 2, message: "yes", severity: 3 } as IDiagnostic],
            instrumentation: [{ stackTrace: "test" }] as IInstrumentation[],
            variableLocations: [{
                name: "a",
                locations: [{
                    startLine: 0,
                    endLine: 1,
                    startColumn: 0,
                    endColumn: 1
                }],
                declaredAt: { start: 0, end: 1 }
            }] as IVariableLocation[]
        };
        const result = {
            salt: 1,
            exception: "e",
            output: [] as string[],
            fullOutput: ["a", "b"],
            succeeded: true,
            diagnostics: [{ start: 1, end: 2, message: "yes", severity: 3 } as IDiagnostic],
            instrumentation: [{ stackTrace: "test" }] as IInstrumentation[],
            currentInstrumentationStep: 0,
            variableLocations: [{
                name: "a",
                locations: [{
                    startLine: 0,
                    endLine: 1,
                    startColumn: 0,
                    endColumn: 1
                }],
                declaredAt: { start: 0, end: 1 }
            }] as IVariableLocation[]

        };

        reducer(originalState, action)
            .should.deep.equal(result).and.not.equal(originalState);
    });

    it("should handle RUN_CODE_RESULT_SPECIFIED and update state", () => {
        const originalState = { salt: 1 };
        const action: any = {
            type: types.RUN_CODE_RESULT_SPECIFIED,
            exception: "e",
            output: ["a", "b"],
            succeeded: true,
            diagnostics: [{ start: 1, end: 2, message: "yes", severity: 3 } as IDiagnostic],
            instrumentation: undefined,
            variableLocations: [] as IVariableLocation[]
        };
        const result = {
            exception: "e",
            output: ["a", "b"],
            fullOutput: ["a", "b"],
            currentInstrumentationStep: 0,
            salt: 1,
            succeeded: true,
            diagnostics: [{ start: 1, end: 2, message: "yes", severity: 3 } as IDiagnostic],
            instrumentation: undefined as IInstrumentation[],
            variableLocations: [] as IVariableLocation[]
        };

        reducer(originalState, action)
            .should.deep.equal(result).and.not.equal(originalState);
    });

    it("should handle RUN_CODE_REQUEST and update state", () => {
        const originalState: IRunState = {
            output: [],
            succeeded: true,
            salt: 1,
            diagnostics: [] as IDiagnostic[]
        };

        const action: any = {
            type: types.RUN_CODE_REQUEST
        };

        reducer(originalState, action)
            .should.deep.equal({ ...initialState, salt: 1 }).and.not.equal(originalState);
    });

    it("advances instrumentation state when NEXT_INSTRUMENTATION_STEP is dispatched", () => {
         reducer( { currentInstrumentationStep: 1 }, {type: types.NEXT_INSTRUMENT_STEP}).currentInstrumentationStep.should.equal(2);
    });

    it("decrements instrumentation state when PREV_INSTRUMENTATION_STEP is dispatched", () => {
        reducer( { currentInstrumentationStep: 1 } , {type: types.PREV_INSTRUMENT_STEP})
            .currentInstrumentationStep.should.equal(0);
    });

    it("changes output when OUTPUT_UPDATED is dispatched", () => {
        reducer({ output: [] }, actions.outputUpdated(["expected"]))
            .output.should.deep.members(["expected"]);
    });

    it("removes instrumentation when UPDATE_WORKSPACE_BUFFER is dispatched", () => {
        reducer({ instrumentation: [], currentInstrumentationStep: 1 }, actions.updateWorkspaceBuffer("", ""))
            .should.deep.equal({ instrumentation: undefined, currentInstrumentationStep: 0});
    });

    it("should handle compile failure and update output with diagnostics", () => {
        const originalState = { salt: 1 };
        const action: Action = {
            type: types.COMPILE_CODE_FAILURE,
            diagnostics: [{end: 0, start:0, message: "stuff", severity: 42}],
            workspaceVersion: 0
            
        };
        const result = {
            salt: 1,
            fullOutput: ["stuff"],
        };

        reducer(originalState, action)
            .should.deep.equal(result).and.not.equal(originalState);
    });
});
