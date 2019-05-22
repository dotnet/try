// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as React from "react";
import * as enzyme from "enzyme";
import * as Adapter from "enzyme-adapter-react-16";
import { shallow } from "enzyme";
import { TryDotnetBanner } from "../../../src/components/TryDotnetBanner";
enzyme.configure({ adapter: new Adapter() });

describe("{ TryDotnetBanner }", () => {
    it(`displays 'Powered by Try .NET' when visible is true`, () => {
        const wrapper = shallow(<TryDotnetBanner />);
        wrapper.find("a").text().should.equal("Powered by Try .NET");
    });

    it(`does not contain any link when visible is false`, () => {
        const wrapper = shallow(<TryDotnetBanner visible={false}/>);
        wrapper.find("a").length.should.equal(0);
    });
});
