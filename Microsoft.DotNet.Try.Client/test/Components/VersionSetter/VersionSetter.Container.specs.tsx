// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as React from "react";
import * as chai from "chai";

import getStore, { IObservableAppStore } from "../../observableAppStore";

import { Action } from "../../../src/constants/ActionTypes";
import { MemoryRouter } from "react-router-dom";
import { Provider } from "react-redux";
import { Route } from "react-router";
import VersionSetter from "../../../src/components/VersionSetter";
import actions from "../../../src/actionCreators/actions";

import * as enzyme from "enzyme";
import * as Adapter from "enzyme-adapter-react-16";
import { mount } from "enzyme";
enzyme.configure({ adapter: new Adapter() });

chai.should();

describe("< VersionSetter />", () => {
    let store: IObservableAppStore;

    beforeEach(() => {
        store = getStore().withDefaultClient();
    });

    it("dispatches the version to the store when it is provided in the route", () => {
        const expectedActions = [
            actions.setVersion(42)
        ];

        mount(
            <Provider store={store}>
                <MemoryRouter initialEntries={["/v42/"]}>
                    <Route path="/v:version(\d+)"
                        component={VersionSetter} />
                </MemoryRouter>
            </Provider>
        );

        store.getActions().should.deep.equal(expectedActions);
    });

    it("does not dispatch the version to the store when it is not provided in the route", () => {
        const expectedActions: Action[] = [
        ];

        mount(
            <Provider store={store}>
                <MemoryRouter initialEntries={["/"]}>
                    <Route path="/v:version(\d+)"
                        component={VersionSetter} />
                </MemoryRouter>
            </Provider>
        );

        store.getActions().should.deep.equal(expectedActions);
    });
});
