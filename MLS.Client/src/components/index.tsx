// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as React from "react";
import * as ReactDOM from "react-dom";

import { BrowserRouter, Route, Switch } from "react-router-dom";

import { AnyAction } from "redux";
import BrowserAdapter, { IHostWindow } from "./BrowserAdapter";
import DotDotNetCodeRunner from "./DotDotNetCodeRunner"
import MsDocsCodeRunner from "./MsDocsCodeRunner";
import VersionSetter from "./VersionSetter";
import AspNetCodeRunner from "./AspNetCodeRunner";

import Cookies = require("js-cookie");
import { ApplicationInsightsClient } from "../ApplicationInsights";
import { IFrameWindow } from "./IFrameWindow";

const Routes = () => {

  return (
  <BrowserRouter>
    <BrowserAdapter
      setCookie={(key, value, options) => Cookies.set(key, value, options)}
      getCookie={(key: string) => Cookies.get(key)}
      log={(m: AnyAction) => console.log(m)}
      hostWindow={new HostWindow(parent)}
      pythiaPercent={1}
      iframeWindow={new IFrameWindow(window)}
      aiFactory={(referrer) => new ApplicationInsightsClient(referrer)} >
      <div>
        <Route path="/v:version(\d+)" component={VersionSetter} />
        <Switch>
          <Route exact path="/:version?/ide" render={() => DotDotNetCodeRunner(window)} />
          <Route exact path="/:version?/editor" render={() => MsDocsCodeRunner(window)} />
          <Route exact path="/:version?/aspnet" component={AspNetCodeRunner} />
        </Switch>
      </div>
    </BrowserAdapter>
  </BrowserRouter>
)};

class HostWindow implements IHostWindow
{
  constructor(private window: Window) 
    {}

  postMessage (message: Object, targetOrigin: string)
  {
    this.window.postMessage(message, targetOrigin);
  }

  getQuery(): URLSearchParams {
    return new URLSearchParams(this.window.location.search);
  }
}

const render = () => {
  ReactDOM.render(
    <Routes />,
    document.getElementById("root")
  );
};

render();
