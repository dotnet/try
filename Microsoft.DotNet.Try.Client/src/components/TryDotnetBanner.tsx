// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as React from "react";
import * as styles from "../dot.net.style.app.css";
import IState from "../IState";
import { connect } from "react-redux";

export interface ITryDotnetBannerProps {
    visible?: boolean;
}

export class TryDotnetBanner extends React.Component<ITryDotnetBannerProps>{
    constructor(props: ITryDotnetBannerProps) {
        super(props);
    }

    public render() {
        let visible = this.props.visible !== false;
        return visible ?
            <a href="https://dotnet.microsoft.com/platform/try-dotnet" target="_blank" className={styles.tryDotnetBannerLink}>Powered by Try .NET</a>
            : null;
    }
}

const mapStateToProps = (state: IState): ITryDotnetBannerProps => {
    let props: ITryDotnetBannerProps = {
        visible: state.ui.enableBranding
    };
    return props;
};

export default connect(mapStateToProps)(TryDotnetBanner);
