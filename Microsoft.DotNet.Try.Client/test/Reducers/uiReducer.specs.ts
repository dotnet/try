// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";
import * as types from "../../src/constants/ActionTypes";

import { IUiState } from "../../src/IState";
import actions from "../../src/actionCreators/actions";
import reducer from "../../src/reducers/uiReducer";

chai.should();

describe("ui Reducer", () => {
    it("should return the initial state", () => {
        reducer(undefined, { type: undefined }).should.deep.equal({
            canShowGitHubPanel: false,
            canEdit: false,
            canRun: false,
            showEditor: true,
            isRunning: false,
            enableBranding: true
        });
    });

    it("should handle HIDE_EDITOR and update state", () => {
        const originalState = {
            salt: 1,
            showEditor: true,
        };
        const action: any = actions.hideEditor();
        const result = {
            salt: 1,
            showEditor: false
        };

        reducer(originalState, action)
            .should.deep.equal(result).and.not.equal(originalState);
    });

    it("should handle SHOW_EDITOR and update state", () => {
        const originalState = {
            salt: 1,
            showEditor: false,
        };
        const action: any = actions.showEditor();
        const result = {
            salt: 1,
            showEditor: true
        };

        reducer(originalState, action)
            .should.deep.equal(result).and.not.equal(originalState);
    });

    let testCases = [
        {
            action: {
                type: types.LOAD_CODE_REQUEST
            },
            expected: {
                canEdit: false,
                canRun: false,
                isRunning: false
            }
        },
        {
            action: {
                type: types.LOAD_CODE_SUCCESS
            },
            expected: {
                canEdit: true,
                canRun: true,
                isRunning: false
            }
        },
        {
            action: {
                type: types.LOAD_CODE_FAILURE
            },
            expected: {
                canEdit: true,
                canRun: true,
                isRunning: false
            }
        },
        {
            action: {
                type: types.RUN_CODE_REQUEST
            },
            expected: {
                canEdit: false,
                canRun: false,
                isRunning: true,
            }
        },
        {
            action: {
                type: types.RUN_CODE_SUCCESS
            },
            expected: {
                canEdit: true,
                canRun: true,
                isRunning: false
            }
        },
        {
            action: {
                type: types.RUN_CODE_FAILURE
            },
            expected: {
                canEdit: true,
                canRun: true,
                isRunning: false
            }
        },
        {
            action: {
                type: types.CAN_SHOW_GITHUB_PANEL,
                canShow: true
            },
            expected: {
                canShowGitHubPanel: true
            }
        },
        {
            action: {
                type: types.CAN_SHOW_GITHUB_PANEL,
                canShow: false
            },
            expected: {
                canShowGitHubPanel: false
            }
        },
        {
            action: {
                type: types.CONFIGURE_ENABLE_INSTRUMENTATION
            },
            expected: {
                instrumentationActive: true
            }
        }
    ];

    testCases.forEach(testCase => {
        let originalState: IUiState = {
            salt: 1,
            canShowGitHubPanel: false,
            canEdit: false,
            canRun: false,
            isRunning: false
        };

        it(`should handle ${testCase.action.type}`, () => {
            reducer(originalState, testCase.action as any)
                .should.deep.equal({ ...originalState, ...testCase.expected, salt: 1 });
        });
    });
});
