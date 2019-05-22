// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as React from "react";

import getStore, { IObservableAppStore } from "../../observableAppStore";

import { Provider } from "react-redux";
import RunButton from "../../../src/components/RunButton";
import actions from "../../../src/actionCreators/actions";
import * as types from "../../../src/constants/ActionTypes";

import { suite } from "mocha-typescript";

import * as enzyme from "enzyme";
import * as Adapter from "enzyme-adapter-react-16";
import { mount } from "enzyme";
import { NullAIClient } from "../../../src/ApplicationInsights";
enzyme.configure({ adapter: new Adapter() });

suite("<RunButton />", () => {
    let store: IObservableAppStore;
    beforeEach(() => {
        store = getStore().withDefaultClient();
    });
    it("dispatches RUN_CLICKED and RUN_REQUEST to the store when clicked", () => {
        store.configure([
            actions.enableClientTelemetry(new NullAIClient()),
            actions.loadCodeSuccess("Console.WriteLine(\"Hello, World\");")
        ]);        
        
        const wrapper = mount(<Provider store={store}>
            <RunButton />
        </Provider>);
        wrapper.find("button").simulate("click");

        let actualActions = store.getActions();//.should.deep.equal(expectedActions);
        actualActions.length.should.equal(2);
        actualActions[0].should.deep.equal(actions.runClicked());
        actualActions[1].type.should.equal(types.RUN_CODE_REQUEST);
    });
    it(`can't be clicked again while running`, () => {
        store.configure([
            actions.enableClientTelemetry(new NullAIClient()),
            actions.loadCodeSuccess("Console.WriteLine(\"Hello, World\");"),
            actions.run()
        ]);

        const wrapper = mount(<Provider store={store}>
            <RunButton />
        </Provider>);
        wrapper.find("button").simulate("click");
        store.getActions().should.deep.equal([]);
    });
});


