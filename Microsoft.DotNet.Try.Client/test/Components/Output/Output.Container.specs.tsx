// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as React from "react";

import getStore, { IObservableAppStore } from "../../observableAppStore";

import Output from "../../../src/components/Output";
import { Provider } from "react-redux";
import actions from "../../../src/actionCreators/actions";
import { suite } from "mocha-typescript";

import * as enzyme from "enzyme";
import * as Adapter from "enzyme-adapter-react-16";
import { mount } from "enzyme";
enzyme.configure({ adapter: new Adapter() });

suite("<Output />", () => {
  var store: IObservableAppStore;
  beforeEach(() => {
    store = getStore();
  });
  it("displays outputLines", () => {
    var expectedOutput = ["some", "output"];
    store.configure([actions.runSuccess({ output: expectedOutput })]);
    const wrapper = mount(<Provider store={store}>
      <Output />
    </Provider>);
    wrapper.text().should.contain("some")
      .and.contain("output");
  });
  it("displays exception", () => {
    var expectedOutput: string[] = [];
    store.configure([actions.runSuccess({ output: expectedOutput, exception: "Oopsies" })]);
    const wrapper = mount(<Provider store={store}>
      <Output />
    </Provider>);
    wrapper.text().should.contain("Unhandled Exception: Oopsies");
  });
});
