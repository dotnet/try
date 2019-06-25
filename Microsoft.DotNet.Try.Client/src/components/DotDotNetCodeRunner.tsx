// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as React from "react";
import * as classnames from "classnames";
import * as styles from "../dot.net.style.app.css";

import Editor from "./Editor";
import Output from "./Output";
import RunButton from "./RunButton";
import InstrumentCheckBox from "./InstrumentCheckBox";
import InstrumentationPanel from "./InstrumentationPanel";
import Running from "./Running";
import GistPanel from "./GistPanel";
import Frame from "./Frame";
import TryDotnetBanner from "./TryDotnetBanner";
import { GetBlazorIFrameUrl } from "./GetBlazorIFrameUrl";

const DotDotNetCodeRunner = (trydotnetWindow: Window) => {
  let url = GetBlazorIFrameUrl(trydotnetWindow);

  // @ts-ignore Some connect type inference thing
  let hack = <Frame src={url}/>;  return (
    <div className={classnames(styles.embeddable)}>
      <div className={classnames(styles.editor)}>
        <div className={classnames(styles.editorDiv)}>
          <Editor />
        </div>
        <div className={classnames(styles.tryDotnetBanner)}>
          <TryDotnetBanner />
        </div>
      </div>
      <div className={classnames(styles.menuBar)}>
        <div>
          <RunButton />
          <InstrumentCheckBox />
        </div>
        <div>
          <Running />
        </div>
      </div>
      <GistPanel />
      <div className={classnames(styles.output)}>
        <Output />
        <InstrumentationPanel />
        {hack}
      </div>
    </div>
  );
};

export default DotDotNetCodeRunner;