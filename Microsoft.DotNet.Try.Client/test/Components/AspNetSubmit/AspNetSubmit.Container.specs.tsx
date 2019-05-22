// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// import "ignore-styles";
import * as React from "react";
import getStore, { IObservableAppStore } from "../../observableAppStore";
import { Provider } from "react-redux";
import AspNetSubmit from "../../../src/components/AspNetSubmit";
import actions from "../../../src/actionCreators/actions";
import * as types from "../../../src/constants/ActionTypes";

import { suite } from "mocha-typescript";

import * as enzyme from "enzyme";
import * as Adapter from "enzyme-adapter-react-16";
import { mount } from "enzyme";
enzyme.configure({ adapter: new Adapter() });

suite("<AspNetSubmit />", () => {
    let store: IObservableAppStore;
    beforeEach(() => {
        store = getStore().withDefaultClient();
    });
    it("dispatches RUN_CLICKED and RUN_REQUEST to the store when clicked", () => {

        store.configure([
            actions.loadCodeSuccess("Console.WriteLine(\"Hello, World\");")
        ]);
        const wrapper = mount(<Provider store={store}>
            <AspNetSubmit />
        </Provider>);
        wrapper.find("button").simulate("click");


        let actualActions = store.getActions();//.should.deep.equal(expectedActions);
        actualActions.length.should.equal(2);
        actualActions[0].should.deep.equal(actions.runClicked());
        actualActions[1].type.should.equal(types.RUN_CODE_REQUEST);
    });
    it("can't be clicked again while running", () => {
        store.configure([
            actions.loadCodeSuccess("Console.WriteLine(\"Hello, World\");"),
            actions.run()
        ]);
        const wrapper = mount(<Provider store={store}>
            <AspNetSubmit />
        </Provider>);
        wrapper.find("button").simulate("click");
        store.getActions().should.deep.equal([]);
    });
    it("forwards the http request to client", () => {
        const httpRequest = {
            uri: "",
            verb: "",
            body: ""
        };

        store.configure([
            actions.loadCodeSuccess("Console.WriteLine(\"Hello, World\");")
        ]);
        const wrapper = mount(<Provider store={store}>
            <AspNetSubmit />
        </Provider>);
        let urlInput = wrapper.find("#uriInput");
        urlInput.simulate("change", { target: { value: httpRequest.uri } });
        let verbInput = wrapper.find("#verbInput");
        verbInput.simulate("change", { target: { value: httpRequest.verb } });
        let bodyInput = wrapper.find("#bodyInput");
        bodyInput.simulate("change", { target: { value: httpRequest.body } });
        wrapper.find("button").simulate("click");

        let actualActions = store.getActions();//.should.deep.equal(expectedActions);
        actualActions.length.should.equal(2);
        actualActions[0].should.deep.equal(actions.runClicked());
        actualActions[1].type.should.equal(types.RUN_CODE_REQUEST);
    });
});


