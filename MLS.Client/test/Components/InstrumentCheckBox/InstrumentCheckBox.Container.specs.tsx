// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as React from "react";
import getStore, { IObservableAppStore } from "../../observableAppStore";
import { Provider } from "react-redux";
import { expect } from "chai";
import { suite } from "mocha-typescript";
import InstrumentCheckBox from "../../../src/components/InstrumentCheckBox";
import actions from "../../../src/actionCreators/actions";

import * as enzyme from "enzyme";
import * as Adapter from "enzyme-adapter-react-16";
import { mount } from "enzyme";
enzyme.configure({ adapter: new Adapter() });

suite("<InstrumentCheckBox />", () => {
    let store: IObservableAppStore;
    beforeEach(() => {
        store = getStore();
    });
    it("is not visisble by default", () => {
        const wrapper = mount(<Provider store={store}>
            <InstrumentCheckBox />
        </Provider>);
        expect(wrapper.html()).to.equal(null);
    });
    it("is visible if instrumentationActive is true", () => {
        store.configure([
            actions.enableInstrumentation()
        ]);
        const wrapper = mount(<Provider store={store}>
            <InstrumentCheckBox />
        </Provider>);
        wrapper.html().should.not.equal(null);
    });
    it("sets instrumentation when checked", () => {
        store.configure([
            actions.enableInstrumentation()
        ]);
        const wrapper = mount(<Provider store={store}>
            <InstrumentCheckBox />
        </Provider>);
        wrapper.find("input").simulate("change");
        store.getActions().should.deep.equal([{ type: "SET_INSTRUMENTATION", enabled: true }]);
    });
});
