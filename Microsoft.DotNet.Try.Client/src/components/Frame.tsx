// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as React from "react";
import IState from "../IState";
import { connect, MapDispatchToProps } from "react-redux";
import { Dispatch } from "redux";
import * as runActions from "../actionCreators/runActionCreators";
import * as notificationActions from "../actionCreators/notificationActionCreators";

export interface IFrameProps {
    isActive?: boolean;
    postMessageData?: object;
    postMessageCallback?: (obj: any) => void;
    sequence?: number;
    src?: URL;
    ready?: (id: string) => void;
    editorId?: string;
}

export interface IFrameState {
    src: URL;
    targetOrigin: string;
}

export class Frame extends React.Component<IFrameProps, IFrameState>
{
    private _frame: HTMLIFrameElement;

    private _callbacks: { [seq: number]: (arg: any) => void } = {};

    constructor(props: IFrameProps) {
        super(props);
        this.state = { src: props.src, targetOrigin: props.src.protocol + "//" +  props.src.host };
    }

    public static defaultProps: IFrameProps =
        {
            postMessageData: null,
            sequence: 0,
            src: null
        };

    public componentWillReceiveProps(nextProps: IFrameProps) {
        if (this.props.sequence !== nextProps.sequence && nextProps.postMessageData) {
            // send a message if postMessageData changed
            this.sendMessage(
                nextProps.postMessageData,
                nextProps.sequence,
                nextProps.postMessageCallback);
        }
    }

    public sendMessage<TResult>(data: any, sequence: number, callback: (arg: TResult) => void) {
        const theThing = { data: data, sequence };
        if (callback) {
            this._callbacks[sequence] = callback;
        }

        this._frame.contentWindow.postMessage(this.serializePostMessageData(theThing), this.state.targetOrigin);
    }

    public componentDidMount() {
        let m = this;
        if (this._frame && this._frame.contentWindow)
        {
            this._frame.contentWindow.addEventListener("message", (ev: MessageEvent) => {
                m.onReceiveMessage(ev);
    
            });
        }
    }

    public onReceiveMessage(event: any) {
        try
        {
            let o = JSON.parse(event.data);
            if (o.sequence && this._callbacks[o.sequence])
            {
                this._callbacks[o.sequence](o.data);
            }

            if (o.ready)
            {
                this.props.ready(this.props.editorId);
            }
        }
       catch
       {          
       }
    }

    public serializePostMessageData(data: any) {
        // To be on the safe side we can also ignore the browser's built-in serialization feature
        // and serialize the data manually.
        if (typeof data === "object") {
            return JSON.stringify(data);
        } else if (typeof data === "string") {
            return data;
        } else {
            return `${data}`;
        }
    }

    public render() {
        const defaultAttributes = {
            allowFullScreen: false,
            height: 1,
            width: 1,
            src: this.state.src.href
        };

        if (this.props.isActive) {
            return (
                <iframe
                        ref={el => {
                            this._frame = el;
                        }}
                        {...defaultAttributes}
                            />
            );
        }
        else {
            return null;
        }
    }
}

export const mapStateToProps = (state: IState): IFrameProps => {

    let newProps: IFrameProps = {
        postMessageData: state.wasmRunner && state.wasmRunner.payload,
        isActive: state.config.useLocalCodeRunner,
        sequence: state.wasmRunner.sequence,
        editorId: state.config.editorId
    };

    newProps.postMessageData = state.wasmRunner && state.wasmRunner.payload;
    newProps.sequence = state.wasmRunner.sequence;
    newProps.postMessageCallback = state.wasmRunner && state.wasmRunner.callback;
    return newProps;
};

const mapDispatchToProps: MapDispatchToProps<IFrameProps, IFrameProps> = (dispatch: Dispatch): IFrameProps => {
    return ({
        ready: (id) => { 
            dispatch(notificationActions.hostRunReady(id));
            dispatch(runActions.wasmRunnerReady(id)); 
        }
    });
};

export default connect(mapStateToProps, mapDispatchToProps)(Frame);
