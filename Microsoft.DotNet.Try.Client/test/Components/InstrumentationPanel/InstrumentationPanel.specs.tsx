// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as React from "react";
import { InstrumentationPanel } from "../../../src/components/InstrumentationPanel";

import * as enzyme from "enzyme";
import * as Adapter from "enzyme-adapter-react-16";
import { shallow } from "enzyme";
enzyme.configure({ adapter: new Adapter() });

describe("{ InstrumentationPanel }", () => {
    it("does not render when not visible", () => {
        const wrapper = shallow(
            <InstrumentationPanel visible={false} canGoNext={true} canGoBack={true} output="" newOutput="" />
        );

        wrapper.isEmptyRender().should.equal(true);
    });

    it("renders program output", () => {
        const wrapper = shallow(
            <InstrumentationPanel  visible={true} canGoNext={false} canGoBack={false} output={"expected"} newOutput="" />
        );

        wrapper.find("p").text().should.equal("expected");
    });

    it("renders new program output", () => {
        const wrapper = shallow(
            <InstrumentationPanel  visible={true} canGoNext={false} canGoBack={false} output="" newOutput={"expected"} />
        );

        wrapper.find("p").text().should.equal("expected");
    });

    it("calls onBack() when back is pressed", () => {
        var clicked = false;
        const wrapper = shallow(
            <InstrumentationPanel  visible={true} canGoNext={true} canGoBack={true} output="" newOutput="" onBack={() => {
                clicked = true;
            }} />
        );
        wrapper.find("#back").simulate("click");
        clicked.should.equal(true);
    });

    it("calls onNext() when next is pressed", () => {
        var clicked = false;
        const wrapper = shallow(
            <InstrumentationPanel  visible={true} canGoNext={true} canGoBack={true} output="" newOutput="" onNext={() => {
                clicked = true;
            }} />
        );
        wrapper.find("#next").simulate("click");
        clicked.should.equal(true);
    });

    it("should disable back when canGoBack is false", () => {
        const wrapper = shallow(
            <InstrumentationPanel  visible={true} canGoNext={false} canGoBack={false} output="" newOutput="" />
        );
        wrapper.find("#back").prop("disabled").should.equal(true);
    });

    it("should disable next when canGoNext is false", () => {
        const wrapper = shallow(
            <InstrumentationPanel  visible={true} canGoNext={false} canGoBack={false} output="" newOutput="" />
        );
        wrapper.find("#next").prop("disabled").should.equal(true);
    });
});
