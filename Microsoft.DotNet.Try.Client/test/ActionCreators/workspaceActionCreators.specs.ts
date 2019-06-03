// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";
import * as types from "../../src/constants/ActionTypes";

import getStore, { IObservableAppStore } from "../observableAppStore";

import MlsClientSimulator from "../mlsClient/mlsClient.simulator";
import actions from "../../src/actionCreators/actions";
import { defaultWorkspace } from "../testResources";
import { configureWorkspace, setInstrumentation, applyScaffolding } from "../../src/actionCreators/workspaceActionCreators";
import { suite } from "mocha-typescript";
import { encodeWorkspace, cloneWorkspace } from "../../src/workspaces";
import { IGistWorkspace } from "../../src/IMlsClient";

chai.should();
chai.use(require("chai-exclude"));

suite("Workspace Action Creators", () => {
    var store: IObservableAppStore;

    beforeEach(() => {
        store = getStore();
    });

    it("initialise with default workspace", () => {
        const expectedActions = [
            actions.setWorkspaceType("script"),
            actions.setWorkspace({
                workspaceType: "script",
                files: [],
                buffers: [{ id: "Program.cs", content: "", position: 0 }],
                usings: [],
            }),
            actions.setActiveBuffer("Program.cs")
        ];
        configureWorkspace(store);
        store.getActions().should.deep.equal(expectedActions);
    });

    it("initialise with workspace parameter", () => {
        const expectedActions = [
            actions.setWorkspaceType("script"),
            actions.setWorkspace(defaultWorkspace),
            actions.setActiveBuffer("Program.cs"),
            actions.setCodeSource("workspace")
        ];
        configureWorkspace(store, encodeWorkspace(defaultWorkspace));
        store.getActions().should.deep.equal(expectedActions);
    });

    it("sets instrumentation", () => {
        const expectedAction = {
         type: types.SET_INSTRUMENTATION,
         enabled: true
        };
        setInstrumentation(true).should.deep.equal(expectedAction);
    });

    it("initialise with from parameter", () => {
        const expectedActions = [
            actions.setWorkspaceType("script"),
            actions.setWorkspace({
                workspaceType: "script",
                files: [],
                buffers: [{ id: "Program.cs", content: "", position: 0 }],
                usings: [],
            }),
            actions.setActiveBuffer("Program.cs"),
            actions.setCodeSource("http://source.com")
        ];
        configureWorkspace(store, undefined, undefined, encodeURIComponent("http://source.com"));
        store.getActions().should.deep.equal(expectedActions);
    });

    it("reports error if both workspace and from parameters are defined and defaults to workspace based initialization", () => {
        var newWorkSpace = cloneWorkspace(defaultWorkspace);
        const expectedActions = [
            actions.error("parameter loading", "cannot define `workspace` and `from` simultaneously"),
            actions.setWorkspaceType("script"),
            actions.setWorkspace(newWorkSpace),
            actions.setActiveBuffer("Program.cs"),
            actions.setCodeSource("workspace")
        ];
        configureWorkspace(store, encodeWorkspace(newWorkSpace), undefined, encodeURIComponent("http://source.com"));
        store.getActions().should.deep.equal(expectedActions);
    });

    it("reports error if both workspace and workspaceType parameters are defined and defaults to workspace based initialization", () => {
        var newWorkSpace = cloneWorkspace(defaultWorkspace);
        const expectedActions = [
            actions.error("parameter loading", "cannot define `workspace` and `workspaceTypeParameter` simultaneously"),
            actions.setWorkspaceType("script"),
            actions.setWorkspace(newWorkSpace),
            actions.setActiveBuffer("Program.cs"),
            actions.setCodeSource("workspace")
        ];
        configureWorkspace(store, encodeWorkspace(newWorkSpace), encodeURIComponent("console"));
        store.getActions().should.deep.equal(expectedActions);
    });

    it("loads a workspace from a Gist URL", async () => {
        const workspaceInfo: IGistWorkspace = {
            htmlUrl: "https://gist.github.com/df44833326fcc575e8169fccb9d41fc7",
            originType: "gist",
            rawFileUrls: [
                {
                    fileName: "Program.cs",
                    url: "https://gist.githubusercontent.com/colombod/df44833326fcc575e8169fccb9d41fc7/raw/35765c05ddb54bc827419211a6b645473cdda7f9/FibonacciGenerator.cs"
                },
                {
                    fileName: "FibonacciGenerator.cs",
                    url: "https://gist.githubusercontent.com/colombod/df44833326fcc575e8169fccb9d41fc7/raw/700a834733fa650d2a663ccd829f8a9d09b44642/Program.cs"
                }],
            workspace: {
                workspaceType: "console",
                buffers: [
                    { id: "Program.cs", content: "console code", position: 0 },
                    { id: "FibonacciGenerator.cs", content: "generator code", position: 0 }],
            }
        };

        const expectedActions = [
            actions.setWorkspaceInfo(workspaceInfo),
            actions.canShowGitHubPanel(false),
            actions.setWorkspace(workspaceInfo.workspace),
            actions.setActiveBuffer("FibonacciGenerator.cs"),
            actions.setCodeSource("workspace"),
            actions.loadCodeRequest("workspace"),
            actions.loadCodeSuccess(workspaceInfo.workspace.buffers[1].content, workspaceInfo.workspace.buffers[1].id)
        ];

        store.configure([
            actions.setClient(new MlsClientSimulator())
        ]);

        await store.dispatch(actions.LoadWorkspaceFromGist("df44833326fcc575e8169fccb9d41fc7", "FibonacciGenerator.cs", "console"));
        store.getActions().should.deep.equal(expectedActions);
    });

    it("loads a workspace from a Gist URL and select first buffer if not speficied", async () => {
        const workspaceInfo: IGistWorkspace = {
            htmlUrl: "https://gist.github.com/df44833326fcc575e8169fccb9d41fc7",
            originType: "gist",
            rawFileUrls: [
                {
                    fileName: "Program.cs",
                    url: "https://gist.githubusercontent.com/colombod/df44833326fcc575e8169fccb9d41fc7/raw/35765c05ddb54bc827419211a6b645473cdda7f9/FibonacciGenerator.cs"
                },
                {
                    fileName: "FibonacciGenerator.cs",
                    url: "https://gist.githubusercontent.com/colombod/df44833326fcc575e8169fccb9d41fc7/raw/700a834733fa650d2a663ccd829f8a9d09b44642/Program.cs"
                }],
            workspace: {
                workspaceType: "console",
                buffers: [
                    { id: "Program.cs", content: "console code", position: 0 },
                    { id: "FibonacciGenerator.cs", content: "generator code", position: 0 }],
            }
        };

        const expectedActions = [
            actions.setWorkspaceInfo(workspaceInfo),
            actions.canShowGitHubPanel(false),
            actions.setWorkspace(workspaceInfo.workspace),
            actions.setActiveBuffer(workspaceInfo.workspace.buffers[0].id),
            actions.setCodeSource("workspace"),
            actions.loadCodeRequest("workspace"),
            actions.loadCodeSuccess(workspaceInfo.workspace.buffers[0].content, workspaceInfo.workspace.buffers[0].id)
        ];

        store.configure([
            actions.setClient(new MlsClientSimulator())
        ]);

        await store.dispatch(actions.LoadWorkspaceFromGist("df44833326fcc575e8169fccb9d41fc7", null, "console"));
        store.getActions().should.deep.equal(expectedActions);
    });

    it("can apply scaffolding.None", () => {
        const expectedActions = [
            actions.setWorkspace({
                activeBufferId: "foo.cs",
                buffers: [{
                    content: "",
                    id: "foo.cs",
                    position: 0
                }],
                files: [{
                    name: "foo.cs",
                    text: ""
                }],
                workspaceType: "script",
            }),
            actions.setActiveBuffer("foo.cs"),
            actions.setCodeSource("workspace"),
            actions.loadCodeRequest("workspace"),
            actions.loadCodeSuccess("", "foo.cs")
        ];
        store.dispatch(applyScaffolding("None", "foo.cs"))
        store.getActions().should.deep.equal(expectedActions);
    });

    it("can apply scaffolding.Class", () => {
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
        store.dispatch(applyScaffolding("Class", "foo.cs", ["System"]))
        store.getActions().should.deep.equal(expectedActions);
    });

    it("can apply scaffolding.Method", () => {
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
                text:`using System;
class C
{
public static void Main()
    {
#region scaffold               
#endregion
    }                
}`
                }],
                workspaceType: "script",
            }),
            actions.setActiveBuffer("foo.cs@scaffold"),
            actions.setCodeSource("workspace"),
            actions.loadCodeRequest("workspace"),
            actions.loadCodeSuccess("", "foo.cs@scaffold")
        ];
        store.dispatch(applyScaffolding("Method", "foo.cs", ["System"]))
        store.getActions().should.deep.equal(expectedActions);
    });
});
