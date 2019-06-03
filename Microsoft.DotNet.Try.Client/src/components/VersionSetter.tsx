// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as React from "react";
import { connect, MapDispatchToProps } from "react-redux";
import { match } from "react-router";
import actions from "../actionCreators/actions";

export interface IVersion {
    version: string;
}

export interface IVersionSetterProps {
    match?: match<IVersion>;
    versionIsSpecified: (version: number) => void;
}

export class VersionSetter extends React.Component<IVersionSetterProps> {
    constructor(props: IVersionSetterProps) {
        super(props);
        
        if (props.match &&
            props.match.params &&
            props.match.params.version &&
            Number(props.match.params.version)) {
            props.versionIsSpecified(Number(props.match.params.version));
        }
    }

    public render(): false { return false; }
}

const mapDispatchToProps: MapDispatchToProps<IVersionSetterProps, IVersionSetterProps> = (dispatch) => {
    return ({
        versionIsSpecified: (version: number) =>
            dispatch(actions.setVersion(version))
    });
};

export default connect(null, mapDispatchToProps)(VersionSetter);
