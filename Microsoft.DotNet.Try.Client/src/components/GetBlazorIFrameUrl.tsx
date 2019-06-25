import { IFrameWindow } from "./IFrameWindow";

export function GetBlazorIFrameUrl(trydotnetWindow: Window) {
  let trydotnetIframe = new IFrameWindow(trydotnetWindow);
  let trydotnetHostOrigin = new URL(trydotnetWindow.document.URL).origin;
  let workspaceType = trydotnetIframe.getQuery().get("workspaceType") || "blazor-console";
  let clientParams = trydotnetIframe.getClientParameters();
  if (clientParams && clientParams.workspaceType) {
    workspaceType = clientParams.workspaceType;
  }
  let localRunnerUrl = `/LocalCodeRunner/${workspaceType}/`;
  let url = new URL(localRunnerUrl, trydotnetHostOrigin);
  url.searchParams.append("embeddingHostOrigin", trydotnetIframe.getHostOrigin().origin);
  url.searchParams.append("trydotnetHostOrigin", trydotnetHostOrigin);
  return url;
}
