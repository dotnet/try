import { GetBlazorIFrameUrl as getBlazorIFrameUrl } from "../src/components/GetBlazorIFrameUrl";
import { JSDOM } from "jsdom";

describe("getBlazorIFrameUrl", () => {
    const documentUrl = "http://try.dot.net";
    const iframeHostOrigin = "http://foo.com";
    const workspaceType = "my_workspace_type";

    let url = new URL(documentUrl);
    url.searchParams.append("hostOrigin", iframeHostOrigin);
    let dom = new JSDOM(`<!DOCTYPE html>
    <html lang="en">
    <body>
        <script id="bundlejs" data-client-parameters="{&quot;workspaceType&quot;:&quot;${workspaceType}&quot;,&quot;useBlazor&quot;:true}" src="/client/bundle.js?v=1.0.0.0"></script>
    </body>

    </html>`,
    {
        url: url.toString(),
        runScripts: "dangerously"
        });
    
    it("returns the url with the workspaceType from the client parameters", () => {
        let url = getBlazorIFrameUrl(dom.window);
        url.pathname.should.be.equal(`/LocalCodeRunner/${workspaceType}/`);
    });

    it("url searchParams has the embeddingHostOrigin  from the iframe HostOrigin", () => {
        let url = getBlazorIFrameUrl(dom.window);
        url.searchParams.getAll("embeddingHostOrigin").should.be.deep.equal([iframeHostOrigin]);
    });

    it("url searchParams has the trydotnetHostOrigin from the document URL", () => {
        let url = getBlazorIFrameUrl(dom.window);
        url.searchParams.getAll("trydotnetHostOrigin").should.be.deep.equal([documentUrl]);
    });
});
