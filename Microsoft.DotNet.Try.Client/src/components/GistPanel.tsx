// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as classnames from "classnames";
import * as React from "react";
import * as styles from "../github.style.app.css";
import IState, { IGistWorkpaceInfo } from "../IState";
import { connect } from "react-redux";
import { ICodeEditorForTryDotNet } from "../constants/ICodeEditorForTryDotNet";

export interface IGistPanelProps {
    showPanel?: boolean;
    htmlUrl?: string;
    rawUrl?: string;
    fileName?: string;
    editor?: ICodeEditorForTryDotNet;
}

export class GistPanel extends React.Component<IGistPanelProps> {

    constructor(props: IGistPanelProps) {
        super(props);
    }

    public componentDidUpdate() {
        const editor = this.props.editor;
        if (editor) {
            editor.layout();
        }
    }

    public componentWillUpdate() {
        const editor = this.props.editor;
        if (editor) {
            editor.layout({ height: 1, width: 1 });
        }
    }

    public render() {
        const showPanel = this.props.showPanel;
        const htmlUrl = this.props.htmlUrl;
        const rawUrl = this.props.rawUrl;
        const fileName = this.props.fileName;

        const gistPanel = {
            width: "100vw",
            paddingLeft: "0px",
            paddingTop: "0px",
            paddingBottom: "0px",
            display: "flex",
            padding: "0px"
        };

        const gistMetaWidth = {
            width: "100vw"
        };

        return showPanel
            ? (<div id="gistPanel" className={classnames(styles.gist)} style={gistPanel}>
                <div className={classnames(styles.gistMeta)} style={gistMetaWidth}>
                    <a id="rawFileUrl" href={rawUrl} style={{ float: "right" }} target={"_blank"}>view raw</a>
                    <a id="webUrl" href={htmlUrl} target={"_blank"}>{fileName}</a> hosted with &#10084; by <a href="https://github.com" target={"_blank"}>GitHub</a>
                </div>
            </div>)
            : (null);
    }
}

const extractFileName = (source: string): string => {
    let found = source.indexOf("@");
    return found >= 0 ? source.slice(0, found) : source;
};

const mapStateToProps = (state: IState): IGistPanelProps => {
    let props: IGistPanelProps = {
        showPanel: false,
        editor: null
    };

    if (state.monaco && state.monaco.editor) {
        props.editor = state.monaco.editor;
    }

    let workspaceInfo = state.workspaceInfo as IGistWorkpaceInfo;

    if (state.ui.canShowGitHubPanel && workspaceInfo && workspaceInfo.originType === "gist") {
        props.htmlUrl = workspaceInfo.htmlUrl;

        let activeFile = extractFileName(state.monaco.bufferId);
        let found = workspaceInfo.rawFileUrls.findIndex(raw => raw.fileName === activeFile);

        if (found >= 0) {
            const fileName = activeFile.split("/").pop();
            props.showPanel = true;
            props.htmlUrl = `${workspaceInfo.htmlUrl}#file-${fileName.replace(".", "-")}`;
            props.rawUrl = workspaceInfo.rawFileUrls[found].url;
            props.fileName = fileName;
        }
    }

    return props;
};

export default connect(mapStateToProps)(GistPanel);
