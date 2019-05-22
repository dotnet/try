// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import actions from "../../src/actionCreators/actions";
import reducer from "../../src/reducers/workspaceReducer";
import { fibonacciCode, emptyWorkspace } from "../testResources";
import { IWorkspace, IWorkspaceState } from "../../src/IState";

function createState(workspace: IWorkspace, useBlazor: boolean = false): IWorkspaceState {
  return { workspace, sequenceNumber: 0, useBlazor: useBlazor };
}

describe("workspace Reducer", () => {
  it("should return the initial state", () => {
    reducer(undefined, undefined).should.deep.equal(createState(emptyWorkspace));
  });

  it("reacts to load code success", () => {
    const action = actions.loadCodeSuccess(fibonacciCode, "Program.cs");

    reducer(createState({ ...emptyWorkspace }), action).workspace.buffers[0].content.should.deep.equal(fibonacciCode);
  });

  it("reacts to displayed code alterations", () => {
    const action = actions.updateWorkspaceBuffer(fibonacciCode, "Program.cs");

    reducer(createState({ ...emptyWorkspace }), action).workspace.buffers[0].content.should.deep.equal(fibonacciCode);
  });

  it("setWorkspace does not replace workspace type if blazor is enabled", () => {
    const newWorkspace = {
      workspaceType: "blazor-blah",
      files: [{ name: "Program.cs", text: fibonacciCode }],
      buffers: [{ id: "", content: fibonacciCode, position: 0 }]
    };
    
    let intialState = createState(newWorkspace);
    let firstState = reducer(intialState, actions.configureBlazor());
    
    const action = actions.setWorkspace({...newWorkspace, workspaceType: "something-bad"});
    let finalState = reducer(firstState, action);

    finalState.workspace.workspaceType.should.equals("blazor-blah");
  });

  it("setWorkspace does replace workspace types when blazor is not enabled", () => {
    const newWorkspace = {
      workspaceType: "something-cool",
      files: [{ name: "Program.cs", text: fibonacciCode }],
      buffers: [{ id: "", content: fibonacciCode, position: 0 }]
    };
    const action = actions.setWorkspace(newWorkspace);

    reducer(createState({ ...emptyWorkspace }), action).workspace.workspaceType.should.equals("something-cool");
  });

  it("setWorkspace replaces non-type values", () => {
    const newWorkspace = {
      workspaceType: "console",
      files: [{ name: "Program.cs", text: fibonacciCode }],
      buffers: [{ id: "", content: fibonacciCode, position: 0 }]
    };
    const action = actions.setWorkspace(newWorkspace);

    let output = reducer(createState({ ...emptyWorkspace }), action).workspace;
    output.workspaceType = newWorkspace.workspaceType;
    output.should.deep.equal(newWorkspace);
  });

  it("can set instrumentation", () => {
    reducer(undefined, actions.setInstrumentation(true))
      .workspace.includeInstrumentation.should.equal(true);
  });

  it("disables instrumentation on code change", () => {
    reducer(createState({ ...emptyWorkspace, includeInstrumentation: true }), actions.updateWorkspaceBuffer("", "Program.cs"))
      .workspace.includeInstrumentation.should.equal(false);
  })

  it("reacts to useBlazor", () => {
    const action = actions.configureBlazor();

    reducer(createState({ ...emptyWorkspace }), action).useBlazor.should.equal(true);
  });
});
