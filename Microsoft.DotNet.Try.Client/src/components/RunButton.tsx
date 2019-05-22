// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as PropTypes from "prop-types";
import * as React from "react";

import { Action } from "redux";
import IState from "../IState";
import actions from "../actionCreators/actions";
import { connect } from "react-redux";
import { ThunkDispatch } from "redux-thunk";

function noop() { }

export interface IRunButtonProps {
    disabled?: boolean;
    onClick?: () => void;
}

export const RunButton: React.SFC<IRunButtonProps> = ({ disabled, onClick }) => (
    <button onClick={onClick} disabled={disabled} >
        Run
          </button>
);

RunButton.propTypes = {
    disabled: PropTypes.bool,
    onClick: PropTypes.func.isRequired
};

RunButton.defaultProps = {
    disabled: false,
    onClick: noop
};

const mapStateToProps = (state: IState) => {
    return ({
        disabled: !state.ui.canRun
    });
};

const mapDispatchToProps = (dispatch: ThunkDispatch<IState, void, Action>) => {
    return ({
        onClick: () => {
            dispatch(actions.runClicked());
            dispatch(actions.run());
        }
    });
};

export default connect(mapStateToProps, mapDispatchToProps)(RunButton);
