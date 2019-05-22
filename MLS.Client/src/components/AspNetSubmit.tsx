// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as React from "react";
import * as classnames from "classnames";
import * as styles from "../asp.net.style.app.css";
import IState from "../IState";
import actions from "../actionCreators/actions";
import { connect } from "react-redux";
import { ThunkDispatch } from "redux-thunk";
import { Action } from "redux";

export interface IHttpRequest {
    uri?: string;
    verb?: string;
    body?: string;
}

export interface IAspNetSubmitProps {
    disabled?: boolean;
    onClick?: (parameters: { httpRequest: IHttpRequest }) => void;
}

export interface IAspNetSubmitState {
    uri?: string;
    verb?: string;
    body?: string;
}

export class AspNetSubmit extends React.Component<IAspNetSubmitProps, IAspNetSubmitState> {
    constructor(props: IAspNetSubmitProps) {
        super(props);
        this.state = { uri: "/", verb: "get", body: "" };

        this.uriDidChange = this.uriDidChange.bind(this);
        this.verbDidChange = this.verbDidChange.bind(this);
        this.bodyDidChange = this.bodyDidChange.bind(this);
        this.submit = this.submit.bind(this);

    }

    private uriDidChange = (uri: string) => {
        this.setState({ uri });
    }

    private verbDidChange = (verb: string) => {
        this.setState({ verb });
    }

    private bodyDidChange = (body: string) => {
        this.setState({ body });
    }

    private submit = () => {
        let parameters = {
            httpRequest: {
                uri: this.state.uri,
                verb: this.state.verb,
                body: this.state.body
            }
        };
        this.props.onClick(parameters);
    }

    public render() {
        let { disabled } = this.props;
        let { uri, verb, body } = this.state;
        return (
            <div className={classnames(styles.panel)}>
                <button onClick={this.submit} disabled={disabled} >Run</button>
                <div className={classnames(styles.httpRequest)}>
                    <div>
                        <label htmlFor="uriInput">relative Uri</label>
                        <input disabled={disabled} onChange={evt => this.uriDidChange(evt.target.value)} id="uriInput" type="text" name="uri" value={uri} />
                    </div>
                    <div>
                        <label htmlFor="verbInput">HTTP verb</label>
                        <input disabled={disabled} onChange={evt => this.verbDidChange(evt.target.value)} id="verbInput" type="text" name="verb" value={verb} />
                    </div>
                    <div>
                        <label htmlFor="bodyInput">Request Body</label>
                        <input disabled={disabled} onChange={evt => this.bodyDidChange(evt.target.value)} id="bodyInput" type="text" name="body" value={body} />
                    </div>                   
                </div>
            </div>
        );
    }
}

const mapStateToProps = (state: IState): IAspNetSubmitProps => {
    return ({
        disabled: !state.ui.canRun
    });
};

const mapDispatchToProps = (dispatch: ThunkDispatch<IState, void, Action>) => {
    return ({
        onClick: (parameters: { httpRequest: IHttpRequest }) => {
            dispatch(actions.runClicked());
            dispatch(actions.run(null,parameters));
        }
    });
};

export default connect(mapStateToProps, mapDispatchToProps)(AspNetSubmit);
