// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as React from "react";
import getStore, { IObservableAppStore } from "../../observableAppStore";
import { Provider } from "react-redux";
import { expect } from "chai";
import { suite } from "mocha-typescript";
import actions from "../../../src/actionCreators/actions";

import * as enzyme from "enzyme";
import * as Adapter from "enzyme-adapter-react-16";
import { mount } from "enzyme";
import Frame from "../../../src/components/Frame";
import * as chai from "chai";
chai.should();

enzyme.configure({ adapter: new Adapter() });

suite("<Frame />", () => {
    let store: IObservableAppStore;
    beforeEach(() => {
        store = getStore();
    });
    it("doesn't load anything by default", () => {
        store.configure([
            actions.notifyHostProvidedConfiguration({
                hostOrigin: new URL("http://try")
            })
        ]);

        // @ts-ignore: Type infererence with connect components
        let hack = <Frame src={new URL("http://jon")} targetOrigin={new URL("http://try")} />;

        const wrapper = mount(
            <Provider store={store}>
                {hack}
            </Provider>);
        expect(wrapper.html()).to.equal(null);
    });
    it("loads stuff if blazor is true", () => {
        store.configure([
            actions.configureBlazor(),
            actions.notifyHostProvidedConfiguration({
                hostOrigin: new URL("http://try")
            })
        ]);

        // @ts-ignore: Type infererence with connect components
        let hack = <Frame src={new URL("http://jon")} targetOrigin={new URL("http://try")} />;
        const wrapper = mount(<Provider store={store}>
            {hack}
        </Provider>);
        wrapper.html().should.not.equal(null);
    });
    
    it("dispatches ready when it receives a message from blazor", () => {
        store.configure([
            actions.configureBlazor(),
            actions.configureEditorId("editor-id"),
            actions.notifyHostProvidedConfiguration({
                hostOrigin: new URL("http://try")
            })
        ]);

        // @ts-ignore: Type infererence with connect components
        let hack = <Frame src={new URL("http://jon")} targetOrigin={new URL("http://try")} />;
        const wrapper = mount(<Provider store={store}>
            {hack}
        </Provider>);

        let frame = wrapper.find("Frame").instance() as any;
        frame.onReceiveMessage({data: JSON.stringify({ready: true})});
        
        store.getActions().should.deep.equal(
            [
                actions.hostRunReady("editor-id"),
                actions.blazorReady("editor-id")]
        );
    });
});
