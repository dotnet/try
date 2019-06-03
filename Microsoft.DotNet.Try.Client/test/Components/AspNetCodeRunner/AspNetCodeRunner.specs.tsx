// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// import "ignore-styles";
import * as chai from "chai";
import * as React from "react";

import AspNetCodeRunner from "../../../src/components/AspNetCodeRunner";
import Editor from "../../../src/components/Editor";
import Output from "../../../src/components/Output";
import Running from "../../../src/components/Running";
import  AspNetSubmit  from "../../../src/components/AspNetSubmit";
import * as enzyme from "enzyme";
import * as Adapter from "enzyme-adapter-react-16";
import { shallow } from "enzyme";
import TryDotnetBanner from "../../../src/components/TryDotnetBanner";

enzyme.configure({ adapter: new Adapter() });
chai.should();

describe("<AspNetCodeRunner />", () => {
    it("contains an <Editor /> component", () => {
        const wrapper = shallow(<AspNetCodeRunner />);

        wrapper.find(Editor).should.have.length(1);
    });

    it("contains a <RunButton /> component", () => {
        const wrapper = shallow(<AspNetCodeRunner />);

        wrapper.find(AspNetSubmit).should.have.length(1);
    });

    it("contains an <Output /> component", () => {
        const wrapper = shallow(<AspNetCodeRunner />);

        wrapper.find(Output).should.have.length(1);
    });

    it("contains an <Running /> component", () => {
        const wrapper = shallow(<AspNetCodeRunner />);

        wrapper.find(Running).should.have.length(1);
    });

    it("contains a <TryDotnetBanner /> component", () => {
        const wrapper = shallow(<AspNetCodeRunner />);

        wrapper.find(TryDotnetBanner).should.have.length(1);
    });
});
