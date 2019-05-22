// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as PropTypes from "prop-types";
import * as React from "react";

import IState from "../IState";
import { connect } from "react-redux";

export interface IOutputProps {
    consoleOutput: string[];
    exception?: string | Error;
    visible?: boolean;
}

export const Output: React.SFC<IOutputProps> = ({ consoleOutput, exception, visible }) => {
    if (visible) {
        return (
            <div>
            {consoleOutput.map((v, i) => <div key={i}>{v}<br /></div>)}
            {
                consoleOutput && consoleOutput.length && exception
                    ? <br />
                    : undefined
            }
            {
                exception
                    ? <div>Unhandled Exception: {exception}</div>
                    : undefined
            }
        </div>
        );
    }
    return null;
};

Output.propTypes = {
    consoleOutput: PropTypes.arrayOf(PropTypes.string),
    visible: PropTypes.bool,
    exception: PropTypes.oneOfType([PropTypes.string,PropTypes.instanceOf(Error)])
};

Output.defaultProps = {
    consoleOutput: [],
    visible: true
};

const mapStateToProps = (state: IState) => {
    return ({
        consoleOutput: state.run.fullOutput,
        exception: state.run.exception,
        visible: !(state.run.instrumentation && state.run.instrumentation.length > 0 && state.workspace.workspace.includeInstrumentation)
    });
};

export default connect(mapStateToProps)(Output);
