// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as PropTypes from "prop-types";
import * as React from "react";

import IState from "../IState";
import { connect } from "react-redux";

export interface IRunningProps {
    visible?: boolean;
}

export const Running: React.SFC<IRunningProps> = ({ visible }) => {
    return visible ? (<div>running...</div>) : null;
};

Running.propTypes = {
    visible: PropTypes.bool
};

Running.defaultProps = {
    visible: false
};

const mapStateToProps = (state: IState) => {
    return ({
        visible: state.ui.isRunning
    });
};

export default connect(mapStateToProps)(Running);
