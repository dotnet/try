// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as React from "react";
import * as classnames from "classnames";
import * as styles from "../mscomdocs.style.app.css";
import Editor from "./Editor";
import GistPanel from "./GistPanel";
import TryDotnetBanner from "./TryDotnetBanner";
import { editorDiv, tryDotnetBanner } from "../dot.net.style.app.css";
import Frame from "./Frame";
import { GetBlazorIFrameUrl } from "./GetBlazorIFrameUrl";

const MsDocsCodeRunner = (trydotnetWindow: Window) => {

  let url = GetBlazorIFrameUrl(trydotnetWindow);

  // @ts-ignore Some connect type inference thing
  let hack = <Frame src={url}/>;
  return (
    <div id="containerRegion" className={`${classnames(styles.embeddable)}`}>
      <div id="editorRegion" className={classnames(styles.editor)}>
        <div className={classnames(editorDiv)}>
            <Editor />
          <div className={classnames(tryDotnetBanner)}>
            <TryDotnetBanner />
          </div>
        </div>
      </div>
      <GistPanel />
      {hack}
    </div>
  );
};

export default MsDocsCodeRunner;
