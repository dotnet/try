// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as React from "react";
import actions from "../../../src/actionCreators/actions";
import getStore, { IObservableAppStore } from "../../observableAppStore";
import Running from "../../../src/components/Running";
import { Provider } from "react-redux";

import * as enzyme from "enzyme";
import * as Adapter from "enzyme-adapter-react-16";
import { mount } from "enzyme";
import { NullAIClient } from "../../../src/ApplicationInsights";
enzyme.configure({ adapter: new Adapter() });

enzyme.configure({ adapter: new Adapter() });

describe("<Running />", () => {
    var store: IObservableAppStore;
    beforeEach(() => {
        store = getStore().withDefaultClient();
    });
    it(`displays 'running...' when running`, () => {
        store.configure([
            actions.enableClientTelemetry(new NullAIClient()),
            actions.loadCodeSuccess("Console.WriteLine(\"Hello, World\");", "https://try.dot.net"),
            actions.run()
        ]);
        const wrapper = mount(<Provider store={store}>
            <Running />
        </Provider>);
        wrapper.find("div").text().should.equal("running...");
    });
    it(`does not display 'running...' when not running`, () => {
        store.configure([
            actions.enableClientTelemetry(new NullAIClient()),
            actions.loadCodeSuccess("abc", "https://try.dot.net")
        ]);
        const wrapper = mount(<Provider store={store}>
            <Running />
        </Provider>);
        wrapper.find("div").length.should.equal(0);
    });
});
