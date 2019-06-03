// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as React from "react";
import * as chai from "chai";

import { IVersionSetterProps, VersionSetter } from "../../../src/components/VersionSetter";

import { MemoryRouter } from "react-router-dom";
import { Route } from "react-router";
import { expect } from "chai";
import * as enzyme from "enzyme";
import * as Adapter from "enzyme-adapter-react-16";
import { mount } from "enzyme";
enzyme.configure({ adapter: new Adapter() });

require("jsdom-global")();

chai.should();

describe("{VersionSetter}", () => {
    it("renders nothing", () => {
        const wrapper = mount(
            <VersionSetter match={undefined} versionIsSpecified={() => { }} />
        );

        expect(wrapper.isEmptyRender()).to.be.true;
    });

    it("invokes the version callback with the version when match includes a version param that is a number", () => {
        var passedVersion: number = -1;

        var versionIsSpecified = (version: number) => { passedVersion = version; };

        mount(
            <MemoryRouter initialEntries={["/v42/"]}>
                <Route path="/v:version(\d+)"
                    component={(props: IVersionSetterProps) =>
                        <VersionSetter {...props}
                            versionIsSpecified={versionIsSpecified} />} />
            </MemoryRouter>
        );

        passedVersion.should.equal(42);
    });

    it("does not invoke the version callback when match includes a version param that is not a number", () => {
        var callbackInvoked: boolean = false;

        var versionIsSpecified = () => { callbackInvoked = true; };

        mount(
            <MemoryRouter initialEntries={["/va_string/"]}>
                <Route path="/v:version"
                    component={(props: IVersionSetterProps) =>
                        <VersionSetter {...props}
                            versionIsSpecified={versionIsSpecified} />} />
            </MemoryRouter>
        );

        expect(callbackInvoked).to.be.false;
    });

    it("does not invoke the version callback when match does not include a version param", () => {
        var callbackInvoked: boolean = false;

        var versionIsSpecified = () => { callbackInvoked = true; };

        mount(
            <MemoryRouter initialEntries={["/"]}>
                <Route path="/v:version(\d+)"
                    component={(props: IVersionSetterProps) =>
                        <VersionSetter {...props}
                            versionIsSpecified={versionIsSpecified} />} />
            </MemoryRouter>
        );

        expect(callbackInvoked).to.be.false;
    });
});
