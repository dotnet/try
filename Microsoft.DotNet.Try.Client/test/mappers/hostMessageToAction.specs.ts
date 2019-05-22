// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as actionTypes from "../../src/constants/ActionTypes";

import { expect, should } from "chai";

import actions from "../../src/actionCreators/actions";
import map from "../../src/mappers/hostMessageToAction";
import sampleThemes from "../sampleThemes";
import { cloneWorkspace } from "../../src/workspaces";
import { emptyWorkspace, defaultGistProject } from "../testResources";
import { IWorkspace } from "../../src/IState";
import getStore from "../observableAppStore";
import { CREATE_REGIONS_FROM_SOURCEFILES_REQUEST, CREATE_PROJECT_FROM_GIST_REQUEST, CREATE_OPERATION_ID_REQUEST, SET_ACTIVE_BUFFER_REQUEST } from "../../src/constants/ApiMessageTypes";

describe("hostMessageToAction mapper", () => {
    it("returns undefined for messages it is not configured to map", () => {
        should().not.exist(map({ type: "some type" }));
    });

    it("maps setRunResult to RUN_CODE_RESULT_SPECIFIED", () => {
        const expectedPayload = {
            output: ["a", "b"],
            succeeded: true
        };

        map({
            type: "setRunResult",
            ...expectedPayload
        }).should.deep.equal({
            type: actionTypes.RUN_CODE_RESULT_SPECIFIED,
            ...expectedPayload
        });
    });

    it("maps setSourceCode to UPDATE_WORKSPACE_BUFFER", () => {

        var store = getStore();

        var sourceCode = "Console.WriteLine(\"Hello, World\");";
        const expectedActions = [actions.updateWorkspaceBuffer("some source code;", "currentBufferId")];
        var ws = cloneWorkspace(emptyWorkspace);
        ws.buffers = [{ id: "currentBufferId", position: 0, content: sourceCode }];

        store.withDefaultClient();
        store.configure([actions.setActiveBuffer("currentBufferId"), actions.setWorkspace(ws)]);

        var mapped = map({
            type: "setSourceCode",
            sourceCode: "some source code;"
        });

        store.dispatch(mapped);
        store.getActions().should.deep.equal(expectedActions);
    });

    it("maps setAdditionalUsings to SET_ADDITIONAL_USINGS", () => {
        const expectedPayload = {
            additionalUsings: ["a_using;"]
        };

        map({
            type: "setAdditionalUsings",
            ...expectedPayload
        }).should.deep.equal({
            type: actionTypes.SET_ADDITIONAL_USINGS,
            ...expectedPayload
        });
    });

    it("maps setWorkspaceFromGist to LOAD_SOURCE", async () => {
        var store = getStore();
        const info = {
            originType: "gist",
            htmlUrl: "https://gist.github.com/df44833326fcc575e8169fccb9d41fc7",
            rawFileUrls:
                [{
                    fileName: "Program.cs",
                    url: "https://gist.githubusercontent.com/colombod/df44833326fcc575e8169fccb9d41fc7/raw/35765c05ddb54bc827419211a6b645473cdda7f9/FibonacciGenerator.cs"
                },
                {
                    fileName: "FibonacciGenerator.cs",
                    url: "https://gist.githubusercontent.com/colombod/df44833326fcc575e8169fccb9d41fc7/raw/700a834733fa650d2a663ccd829f8a9d09b44642/Program.cs"
                }],
            workspace:
            {
                workspaceType: "console",
                buffers:
                    [
                        { id: "Program.cs", content: "console code", position: 0 },
                        { id: "FibonacciGenerator.cs", content: "generator code", position: 0 }
                    ],
            }
        };

        const expectedActions = [
            actions.setWorkspaceInfo(info),
            actions.canShowGitHubPanel(false),
            actions.setWorkspace(info.workspace),
            actions.setActiveBuffer("FibonacciGenerator.cs"),
            actions.setCodeSource("workspace"),
            actions.loadCodeRequest("workspace"),
            actions.loadCodeSuccess(info.workspace.buffers[1].content, info.workspace.buffers[1].id)
        ];

        var mapped = map({
            type: "setWorkspaceFromGist",
            gistId: "df44833326fcc575e8169fccb9d41fc7",
            bufferId: "FibonacciGenerator.cs",
            workspaceType: "console"
        });

        store.withDefaultClient();
        await store.dispatch(mapped);

        store.getActions().should.deep.equal(expectedActions);
    });

    it("maps setWorkspace to LOAD_SOURCE", async () => {
        var store = getStore();

        let workspace: IWorkspace = {
            workspaceType: "console",
            files: [{
                name: "Program.cs",
                text: "using System;\nusing System.Linq;\n\nnamespace FibonacciTest\n{\n public class Program\n {\n public static void Main()\n {\n #region codeRegion\n #endregion\n }\n }\n}"
            }],
            buffers: [{
                id: "Program.cs@codeRegion",
                content: "Console.WriteLine(\"something new here\")",
                position: 0
            }],

        };

        const expectedActions = [
            actions.setWorkspace(workspace),
            actions.setActiveBuffer("Program.cs@codeRegion"),
            actions.setCodeSource("workspace"),
            actions.loadCodeRequest("workspace"),
            actions.loadCodeSuccess(workspace.buffers[0].content, workspace.buffers[0].id)
        ];

        var mapped = map({
            type: "setWorkspace",
            workspace: workspace,
            bufferId: "Program.cs@codeRegion",
        });

        store.withDefaultClient();
        await store.dispatch(mapped);

        store.getActions().should.deep.equal(expectedActions);
    });

    it("maps run to RUN_CODE_REQUEST", async () => {
        var store = getStore();

        const expectedActions = [
            actions.runRequest("Test_Run"),
            actions.runSuccess({
                output: ["Hello, World"],
                succeeded: true
            })
        ];

        var sourceCode = "Console.WriteLine(\"Hello, World\");";
        var ws = cloneWorkspace(emptyWorkspace);
        ws.buffers = [{ id: "Program.cs", position: 0, content: sourceCode }];

        store.withDefaultClient();
        store.configure([actions.setWorkspace(ws)]);

        var mapped = map({
            type: "run",
            requestId: "Test_Run"
        });

        await store.dispatch(mapped);

        store.getActions().should.deep.equal(expectedActions);
    });

    it("maps setEditorOptions to CONFIGURE_MONACO_EDITOR", () => {
        const expectedPayload = {
            editorOptions: { some: "option" },
            theme: "some string"
        };

        map({
            type: "configureMonacoEditor",
            ...expectedPayload
        }).should.deep.equal({
            type: actionTypes.CONFIGURE_MONACO_EDITOR,
            ...expectedPayload
        });
    });

    it("maps showEditor to SHOW_EDITOR", () => {
        map({
            type: "showEditor"
        }).should.deep.equal({
            type: actionTypes.SHOW_EDITOR
        });
    });

    it("maps focus to focusMonacoEditor", () => {
        var store = getStore();
        var focusInvoked = false;

        store.configure([actions.notifyMonacoReady({ focus: () => { focusInvoked = true; }, layout: () => { } })]);

        var mapped = map({
            type: "focusEditor"
        });

        store.dispatch(mapped);

        expect(focusInvoked).to.be.true;
    });

    it("maps defineMonacoEditorThemes to DEFINE_MONACO_EDITOR_THEMES", () => {
        const expectedPayload = {
            themes: [sampleThemes.myCustomTheme]
        };

        map({
            type: "defineMonacoEditorThemes",
            themes: [sampleThemes.myCustomTheme]
        }).should.deep.equal({
            type: actionTypes.DEFINE_MONACO_EDITOR_THEMES,
            ...expectedPayload
        });
    });

    it("maps setInstrumentation to SET_INSTRUMENTATION", () => {
        map({
            type: "setInstrumentation",
            enabled: true
        }).should.deep.equal({
            type: actionTypes.SET_INSTRUMENTATION,
            enabled: true
        });
    });

    it("maps checkPrevEnabled", () => {
        const store = getStore();
        store.withDefaultClient();
        store.configure([actions.runSuccess({
            output: ["before after"],
            instrumentation: [
                {
                    output: {
                        start: 0,
                        end: 6
                    }
                }]
        })]);

        store.dispatch(
            map({
                type: "checkPrevEnabled"
            })
        );
        store.getActions().should.deep.equal([{
            type: actionTypes.CANNOT_MOVE_PREV
        }]);
    });

    it("maps checkNextEnabled", () => {
        const store = getStore();
        store.configure([actions.runSuccess({
            output: ["before after"],
            instrumentation: [
                {
                    output: {
                        start: 0,
                        end: 6
                    }
                }]
        })]);

        store.dispatch(
            map({
                type: "checkNextEnabled"
            })
        );
        store.getActions().should.deep.equal([{
            type: actionTypes.CANNOT_MOVE_NEXT
        }]);
    });
    it("maps prevInstrumentationStep", async () => {
        const store = getStore();
        store.configure([actions.runSuccess({
            output: ["before after"],
            instrumentation: [
                {
                    output: {
                        start: 0,
                        end: 6
                    }
                }, {
                    output: {
                        start: 6,
                        end: 12
                    }
                }]
        })]);

        const next = map({ type: "nextInstrumentationStep" });
        const back = map({ type: "prevInstrumentationStep" });

        store.dispatch(next);
        store.dispatch(back);

        store.getActions().should.deep.include.members([{
            type: actionTypes.PREV_INSTRUMENT_STEP
        }, {
            type: actionTypes.OUTPUT_UPDATED,
            output: ["before"]
        }, {
            type: actionTypes.CANNOT_MOVE_PREV
        }]);
    });

    it("maps nextInstrumentationStep", async () => {
        const store = getStore();
        store.configure([actions.runSuccess({
            output: ["before after"],
            instrumentation: [
                {},
                {
                    output: {
                        start: 0,
                        end: 6
                    }
                }]
        })]);

        const mapped = map({ type: "nextInstrumentationStep" });
        store.dispatch(mapped);

        store.getActions().should.deep.equal([{
            type: actionTypes.NEXT_INSTRUMENT_STEP
        }, {
            type: actionTypes.OUTPUT_UPDATED,
            output: ["before"]
        }, {
            type: actionTypes.CANNOT_MOVE_NEXT
        }]);
    });

    it("maps applyScaffolding", async () => {
        var store = getStore();

        const expectedActions = [
            actions.setWorkspace({
                activeBufferId: "foo.cs@scaffold",
                buffers: [{
                    content: "",
                    id: "foo.cs@scaffold",
                    position: 0
                }],
                files: [{
                    name: "foo.cs",
                    text: `using System;
class C
{
#region scaffold               
#endregion                  
}`
                }],
                workspaceType: "script",
            }),
            actions.setActiveBuffer("foo.cs@scaffold"),
            actions.setCodeSource("workspace"),
            actions.loadCodeRequest("workspace"),
            actions.loadCodeSuccess("", "foo.cs@scaffold")
        ];

        var mapped = map({
            type: "applyScaffolding",
            fileName: "foo.cs",
            scaffoldingType: "Class",
            additionalUsings: ["System"]
        });

        store.withDefaultClient();
        await store.dispatch(mapped);

        store.getActions().should.deep.equal(expectedActions);
    });

    it("maps generateOperationId to OPERATION_ID_GENERATED", () => {
        let action = map({
            type: CREATE_OPERATION_ID_REQUEST,
            requestId: "Test_Run"
        });
        action.type.should.equal(actionTypes.OPERATION_ID_GENERATED);
        action.requestId.should.equal("Test_Run");
        action.operationId.should.not.equal(null);
    });

    it("maps createprojectfromgist to CREATE_PROJECT_SUCCESS", async () => {
        var store = getStore();

        const expectedActions = [
            {
                type: actionTypes.CREATE_PROJECT_SUCCESS,
                requestId: "Test_Run",
                project: { ...defaultGistProject }
            }
        ];

        store.withDefaultClient();

        var mapped = map({
            type: CREATE_PROJECT_FROM_GIST_REQUEST,
            requestId: "Test_Run",
            gistId: "any",
            projectTemplate: defaultGistProject.projectTemplate
        });

        await store.dispatch(mapped);

        store.getActions().should.deep.equal(expectedActions);
    });

    it("maps generateregionfromfiles to GENERATE_REGIONS_FROM_FILES_SUCCESS", async () => {
        var store = getStore();

        const expectedActions = [
            {
                type: actionTypes.CREATE_REGIONS_FROM_SOURCEFILES_SUCCESS,
                requestId: "Test_Run",
                regions: [{ id: "code.cs@left", content: "right" }]
            }
        ];

        store.withDefaultClient();

        var mapped = map({
            type: CREATE_REGIONS_FROM_SOURCEFILES_REQUEST,
            requestId: "Test_Run",
            files: [{ name: "code.cs", content: "left$$right" }]
        });

        await store.dispatch(mapped);

        store.getActions().should.deep.equal(expectedActions);
    });

    it("maps SET_ACTIVE_BUFFER_REQUEST to SET_ACTIVE_BUFFER", () => {

        var store = getStore();

        var sourceCode = "Console.WriteLine(\"Hello, World\");";
        const expectedActions = [actions.setActiveBuffer("new bufferId")];
        var ws = cloneWorkspace(emptyWorkspace);
        ws.buffers = [{ id: "currentBufferId", position: 0, content: sourceCode },{ id: "new bufferId", position: 0, content: sourceCode }];

        store.withDefaultClient();
        store.configure([actions.setActiveBuffer("currentBufferId"), actions.setWorkspace(ws)]);

        var mapped = map({
            type: SET_ACTIVE_BUFFER_REQUEST,
            bufferId: "new bufferId"
        });

        store.dispatch(mapped);
        store.getActions().should.deep.equal(expectedActions);
    });
});
