// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as React from "react";
import getStore, { IObservableAppStore } from "../../observableAppStore";
import { Provider } from "react-redux";
import { expect } from "chai";
import { suite } from "mocha-typescript";
import InstrumentationPanel from "../../../src/components/InstrumentationPanel";
import actions from "../../../src/actionCreators/actions";

import * as enzyme from "enzyme";
import * as Adapter from "enzyme-adapter-react-16";
import { mount } from "enzyme";
enzyme.configure({ adapter: new Adapter() });

suite("<InstrumentationPanel />", () => {
    var store: IObservableAppStore;
    beforeEach(() => {
        store = getStore();
    });
    it("is not visible by default", () => {
        const wrapper = mount(<Provider store={store}>
            <InstrumentationPanel />
        </Provider>);
        expect(wrapper.html()).to.equal(null);
    });
    it("is visible after a runrequest with instrumentation", () => {
        store.configure([
            actions.runSuccess({
                output: ["hello world"],
                instrumentation: [{
                    output: {
                        "start": 0,
                        "end": 0
                    }
                }]
            }),
            actions.setInstrumentation(true)
        ]);
        const wrapper = mount(<Provider store={store}>
            <InstrumentationPanel />
        </Provider>);
        wrapper.html().should.not.equal(null);
    });
    it("shows new output", () => {
        store.configure([
            actions.runSuccess({
                output: ["before after"],
                instrumentation: [{
                    output: {
                        start: 0,
                        end: 6
                    }
                }]
            }),
            actions.setInstrumentation(true)
        ]);
        const wrapper = mount(<Provider store={store}>
            <InstrumentationPanel />
        </Provider>);
        wrapper.find("#newOutput").text().should.equal("before");
    });
    it("shows previous output when instrumentation advances", () => {
        store.configure([actions.runSuccess({
            output: ["before after"],
            instrumentation: [{
                output: {
                    start: 0,
                    end: 6
                }
            },
            {
                output: {
                    start: 6,
                    end: 12
                }
            }]
        }),
        actions.setInstrumentation(true),
        actions.nextInstrumentationStep()]);
        const wrapper = mount(<Provider store={store}>
            <InstrumentationPanel />
        </Provider>);
        wrapper.find("#output").text().should.equal("before");
    });
});
