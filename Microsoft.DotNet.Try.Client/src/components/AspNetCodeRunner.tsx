// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as React from "react";
import * as classnames from "classnames";
import * as styles from "../dot.net.style.app.css";

import Editor from "./Editor";
import Output from "./Output";
import AspNetSubmit from "./AspNetSubmit";
import Running from "./Running";
import GistPanel from "./GistPanel";
import TryDotnetBanner from "./TryDotnetBanner";

const AspNetCodeRunner = () => {
  return (    
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
          <AspNetSubmit />
        </div>
        <div>
          <Running />
        </div>
      </div>
      <GistPanel />
      <div className={classnames(styles.output)}>
        <Output />
      </div>
    </div>
  );
};

export default AspNetCodeRunner;
