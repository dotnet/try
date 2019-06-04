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

export interface IInstrumentationPanelProps {
    visible: boolean;
    canGoNext: boolean;
    canGoBack: boolean;
    output: string;
    newOutput: string;
    onNext?: () => void;
    onBack?: () => void;
}

export const InstrumentationPanel: React.SFC<IInstrumentationPanelProps> = ({ visible, canGoNext, canGoBack, output, newOutput, onNext, onBack }) => {
    if (visible) {
        return (
            <div>
                <div>
                    <button id="back" disabled={!canGoBack} onClick={onBack}> Back</button>
                    <button id="next" disabled={!canGoNext} onClick={onNext} > Next</button>
                </div>
                <div>
                    <p style={{ whiteSpace: "pre-line" }}>
                        <span id="output">{output}</span>
                        <span id="newOutput" style={{ color: "red" }}>{newOutput}</span>
                    </p>
                </div>
            </div>
        );
    }
    return null;
};

InstrumentationPanel.propTypes = {
    visible: PropTypes.bool,
    canGoNext: PropTypes.bool,
    canGoBack: PropTypes.bool,
    output: PropTypes.string,
    newOutput: PropTypes.string,
    onNext: PropTypes.func,
    onBack: PropTypes.func
};

const defaultNonActionProps = {
    visible: false,
    canGoNext: false,
    canGoBack: false,
    output: "",
    newOutput: ""
};

InstrumentationPanel.defaultProps = {
    ...defaultNonActionProps,
    onNext: noop,
    onBack: noop
};

const mapStateToProps = (state: IState) => {

    if (!state.run.instrumentation || state.run.instrumentation.length === 0 || !state.workspace.workspace.includeInstrumentation) {
        return defaultNonActionProps;
    }

    var currentStep = state.run.instrumentation[state.run.currentInstrumentationStep];

    var rawOutput = state.run.fullOutput.join("\n") || "";
    var canGoNext = state.run.currentInstrumentationStep < state.run.instrumentation.length - 1;
    var canGoPrev = state.run.currentInstrumentationStep > 0;

    var output = rawOutput.substring(0, currentStep.output.start);
    var newOutput = rawOutput.substring(currentStep.output.start, currentStep.output.end);

    return ({
        visible: true,
        canGoNext: canGoNext,
        canGoBack: canGoPrev,
        output: output,
        newOutput: newOutput
    });
};

const mapDispatchToProps = (dispatch: ThunkDispatch<IState, void, Action>) => {
    return ({
        onNext: () => { dispatch(actions.nextInstrumentationStep()); },
        onBack: () => { dispatch(actions.prevInstrumentationStep()); }
    });
};

export default connect(mapStateToProps, mapDispatchToProps)(InstrumentationPanel);
