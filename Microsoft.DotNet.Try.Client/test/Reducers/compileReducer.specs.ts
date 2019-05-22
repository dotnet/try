// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as types from "../../src/constants/ActionTypes";

import { ICompileState, IDiagnostic } from "../../src/IState";
import compileReducer from "../../src/reducers/compileReducer";
import { compileSuccess, compileFailure } from "../../src/actionCreators/runActionCreators";

const initialState: ICompileState = {
    diagnostics: undefined,
    base64assembly: undefined,
    succeeded: undefined,
    workspaceVersion: undefined
};

describe("compile Reducer", () => {
    it("should return the initial state", () => {
        compileReducer(undefined, { type: undefined }).should.deep.equal(initialState);

    });

    it("should handle COMPILE_CODE_SUCCESS and update state", () => {
        const originalState = initialState;
        const action = compileSuccess(
            {
                base64assembly: "foo",
                
            },
            1
        );

        const result : ICompileState = {
            base64assembly: "foo",
            diagnostics: undefined,
            succeeded: true,
            workspaceVersion: 1

        };

        compileReducer(originalState, action)
            .should.deep.equal(result).and.not.equal(originalState);
    });

    it("should handle COMPILE_CODE_FAILURE and update state", () => {
        const originalState = {...initialState, salt: 1 };
        const action = compileFailure(
            {
                succeeded: false,
                diagnostics: [{ start: 1, end: 2, message: "yes", severity: 3 } as IDiagnostic],
                base64assembly: undefined
            },
            1
        );

        const result = {
            succeeded: false,
            base64assembly: undefined as string,
            salt: 1,
            diagnostics: [{ start: 1, end: 2, message: "yes", severity: 3 } as IDiagnostic],
            workspaceVersion: 1
        };

        compileReducer(originalState, action)
            .should.deep.equal(result).and.not.equal(originalState);
    });

    it("should handle COMPILE_CODE_REQUEST and update state", () => {
        const originalState = {
            ...initialState,
            base64assembly: "",
            salt: 1
        };

        const action: any = {
            type: types.COMPILE_CODE_REQUEST
        };

        compileReducer(originalState, action)
            .should.deep.equal({ ...initialState, salt: 1 }).and.not.equal(originalState);
    });
});
