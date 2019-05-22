// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


import * as React from "react";
import actions from "../../../src/actionCreators/actions";
import getStore, { IObservableAppStore } from "../../observableAppStore";
import { Provider } from "react-redux";

import * as enzyme from "enzyme";
import * as Adapter from "enzyme-adapter-react-16";
import { mount } from "enzyme";
import TryDotnetBanner from "../../../src/components/TryDotnetBanner";
enzyme.configure({ adapter: new Adapter() });

describe("<TryDotnetBanner />", () => {
    var store: IObservableAppStore;
    
    beforeEach(() => {
        store = getStore().withDefaultClient();
    });

    it(`displays 'Powered by Try .NET...' when branding is enabled`, () => {
        store.configure([
            actions.enableBranding()
        ]);
        
        const wrapper = mount(<Provider store={store}>
            <TryDotnetBanner />
        </Provider>);

        wrapper.find("a").text().should.equal("Powered by Try .NET");
    });

    it(`does not display 'Powered by Try .NET...' when branding is disabled`, async() => {
        store.configure([
            actions.disableBranding()
        ]);

        const wrapper = mount(<Provider store={store}>
            <TryDotnetBanner />
        </Provider>);

        wrapper.find("a").length.should.equal(0);
    });
});
