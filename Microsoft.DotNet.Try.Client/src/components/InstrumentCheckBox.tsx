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

export interface IInstrumentCheckBoxProps {
    visible: boolean;
    checked: boolean;
    disabled?: boolean;
    onChanged?: (newValue: boolean) => void;
}

export const InstrumentCheckBox: React.SFC<IInstrumentCheckBoxProps> = ({ visible, disabled, checked, onChanged }) => {
    if (visible) {
        return (
            <label>
                Instrument?
            <input type="checkbox" disabled={disabled} checked={checked} onChange={() => onChanged(!checked)} />
            </label>
        );
    }
    return null;
};

InstrumentCheckBox.propTypes = {
    visible: PropTypes.bool,
    disabled: PropTypes.bool,
    checked: PropTypes.bool,
    onChanged: PropTypes.func.isRequired
};

InstrumentCheckBox.defaultProps = {
    disabled: false,
    checked: false,
    onChanged: noop
};

const mapStateToProps = (state: IState) => {
    return ({
        visible: state.ui.instrumentationActive,
        disabled: !state.ui.canRun,
        checked: state.workspace.workspace.includeInstrumentation
    });
};

const mapDispatchToProps = (dispatch: ThunkDispatch<IState, void, Action>) => {
    return ({
        onChanged: (newValue: boolean) => {
            dispatch(actions.setInstrumentation(newValue));
        }
    });
};

export default connect(mapStateToProps, mapDispatchToProps)(InstrumentCheckBox);
