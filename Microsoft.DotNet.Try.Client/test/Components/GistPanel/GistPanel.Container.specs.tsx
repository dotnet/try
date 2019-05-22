// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// import "ignore-styles";
import * as React from "react";
import getStore, { IObservableAppStore } from "../../observableAppStore";
import GistPanel from "../../../src/components/GistPanel";
import { Provider } from "react-redux";
import actions from "../../../src/actionCreators/actions";
import { mount } from "enzyme";
import { should, expect } from "chai";
import { suite } from "mocha-typescript";
import { IGistWorkpaceInfo } from "../../../src/IState";

should();

suite("<GistPanel />", () => {
    var store: IObservableAppStore;

    beforeEach(() => {
        store = getStore();
    });

    it("is not rendered if workspace info is not  gist", () => {
        store.configure([
            actions.setActiveBuffer("file.cs@regionOne")
        ]);

        store.dispatch(actions.setWorkspaceInfo({ originType: "notGist" }));
        const wrapper = mount(
            <Provider store={store}>
                <GistPanel />
            </Provider>);
        expect(wrapper.html()).to.be.null;
    });

    it("is rendered if workspace info is gist", () => {
        const workspaceInfo: IGistWorkpaceInfo = {
            originType: "gist",
            htmlUrl: "gistUrl",
            rawFileUrls: [{ fileName: "fileA.cs", url: "fileOneUrl" }]
        };

        store.configure([
            actions.canShowGitHubPanel(true),
            actions.setActiveBuffer("fileA.cs@regionOne")
        ]);

        store.dispatch(actions.setWorkspaceInfo(workspaceInfo));
        const wrapper = mount(
            <Provider store={store}>
                <GistPanel />
            </Provider>);

        wrapper
            .find("#rawFileUrl")
            .props().href.should.be.equal("fileOneUrl");

        wrapper
            .find("#webUrl")
            .props().href.should.be.equal("gistUrl#file-fileA-cs");
    });
});
