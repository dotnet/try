// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";
import { JSDOM } from "jsdom";
import { PreOutputPanel } from "../src";
import { isMainThread } from "worker_threads";

chai.should();

describe("a preOutputPanel", () => {
    let dom: JSDOM = null;

    beforeEach(() => {
        dom = new JSDOM(
            `<!DOCTYPE html>
            <html lang="en">
            <body>
            <div class="size-to-content"><div id="outputPanel" ></div></div>
                
            </body>
            </html>`,
            {
                url: "http://localhost",
                runScripts: "dangerously"
            });
    });

    it("can clear the content", async () => {
        let div = dom.window.document.querySelector<HTMLDivElement>(`#outputPanel`);
        let panel = new PreOutputPanel(div);
        await panel.clear();
        div.innerHTML.should.be.equal("<pre><code></code></pre>");
    });

    it("can set the content", async () => {
        let div = dom.window.document.querySelector<HTMLDivElement>(`#outputPanel`);
        let panel = new PreOutputPanel(div);
        await panel.write("new Entry");
        div.innerHTML.should.be.equal("<pre><code>new Entry</code></pre>");
    });

    it("can set the content from a list of strings", async () => {
        let div = dom.window.document.querySelector<HTMLDivElement>(`#outputPanel`);
        let content = ["a", "b", "c"];
        let expectedContent = 
`<pre><code>a
b
c</code></pre>`;
        let panel = new PreOutputPanel(div);
        await panel.write(content);
        div.innerHTML.should.be.equal(expectedContent);
    });

    it("can append to the content", async () => {
        let div = dom.window.document.querySelector<HTMLDivElement>(`#outputPanel`);
        let panel = new PreOutputPanel(div);
        var oldEntry = "old Entry";
        var newEntry = "new Entry";
        await panel.append(oldEntry);
        await panel.append(newEntry);
        let expectedContent = `<pre><code>${oldEntry}${newEntry}</code></pre>`;
        div.innerHTML.should.be.equal(expectedContent);
    });

    it("can append to the content from a list of strings", async () => {
        let div = dom.window.document.querySelector<HTMLDivElement>(`#outputPanel`);
        let oldEntry = "old Entry";
        let content = ["a", "b", "c"];
        let expectedContent =
`<pre><code>${oldEntry}a
b
c</code></pre>`;
        let panel = new PreOutputPanel(div);
        await panel.append(oldEntry)
        await panel.append(content);
        div.innerHTML.should.be.equal(expectedContent);
    });

    it("html encodes the appended content", async () => {
        let div = dom.window.document.querySelector<HTMLDivElement>(`#outputPanel`);
        let content = ["<div>code</div>", "b", "c"];
        let expectedContent = 
`<pre><code>&lt;div&gt;code&lt;/div&gt;
b
c</code></pre>`;
        let panel = new PreOutputPanel(div);
        await panel.append(content);
        div.innerHTML.should.be.equal(expectedContent);
    });

    it("preserves white spaces", async () => {
        let div = dom.window.document.querySelector<HTMLDivElement>(`#outputPanel`);
        let content = ["   3 whitespaces before", "3 whitespaces after   "];
        let expectedContent = 
`<pre><code>   3 whitespaces before
3 whitespaces after   </code></pre>`;
        let panel = new PreOutputPanel(div);
        await panel.append(content);
        div.innerHTML.should.be.equal(expectedContent);
    });

    it("if the content is cleared and append is called, it retains the new content", async() => {
        let div = dom.window.document.querySelector<HTMLDivElement>(`#outputPanel`);
        let panel = new PreOutputPanel(div);
        await panel.append("old content");
        await panel.clear();
        await panel.write("new content");
        div.innerHTML.should.be.equal("<pre><code>new content</code></pre>");
    });

    describe("it can size-to-content", () => {
        describe("write", () => {
            it("if the length of the content is less than 5, it sets the height to 5em", async() => {
                let div = dom.window.document.querySelector<HTMLDivElement>(`#outputPanel`);
                let panel = new PreOutputPanel(div);
               div.parentElement.style.height.should.be.empty;
                await panel.write("Hello World");
               div.parentElement.style.height.should.be.equal("5em");
            });
        
            it("if the length of the content is greater than 5, it sets the height to the content", async() => {
                let div = dom.window.document.querySelector<HTMLDivElement>(`#outputPanel`);
                let content = ["Line 1", "Line 2", "Line 3", "Line 4", "Line 5", "Line 6", "Line 7"];
                let panel = new PreOutputPanel(div);
               div.parentElement.style.height.should.be.empty;
                await panel.write(content);
               div.parentElement.style.height.should.be.equal("7em");
            });

            it("if the string contains break lines, the height is set accordingly", async() => {
                let div = dom.window.document.querySelector<HTMLDivElement>(`#outputPanel`);
                let panel = new PreOutputPanel(div);
               div.parentElement.style.height.should.be.empty;
                await panel.write("Line 1\nLine 2\nLine 3\n\nLine 4\nLine 5\nLine 6\nLine 7");
               div.parentElement.style.height.should.be.equal("8em");
            });
        
            it("if the div doesnt have the size-to-content class, it doesnt modify the height", async() => {
                let dom = new JSDOM(
                    `<!DOCTYPE html>
                    <html lang="en">
                    <body>
                        <div id="outputPanel"></div>
                    </body>
                    </html>`,
                    {
                        url: "http://localhost",
                        runScripts: "dangerously"
                    });
                let div = dom.window.document.querySelector<HTMLDivElement>(`#outputPanel`);
                let content = ["Line 1", "Line 2", "Line 3", "Line 4", "Line 5", "Line 6", "Line 7"];
                let panel = new PreOutputPanel(div);
                await panel.write(content);
               div.parentElement.style.height.should.be.empty;
            });

            it("once the height has been set it doesnt change in the next call", async() => {
                let div = dom.window.document.querySelector<HTMLDivElement>(`#outputPanel`);
                let panel = new PreOutputPanel(div);
                let content = ["Line 1", "Line 2"];
                await panel.write(content);
               div.parentElement.style.height.should.be.equal("5em");
                content = ["Line 1", "Line 2", "Line 3", "Line 4", "Line 5", "Line 6"];
                await panel.write(content);
               div.parentElement.style.height.should.be.equal("5em");
            });
        });

        describe("append", () => {
            it("if the length of the content is less than 5, it sets the height to 5em", async() => {
                let div = dom.window.document.querySelector<HTMLDivElement>(`#outputPanel`);
                let panel = new PreOutputPanel(div);
               div.parentElement.style.height.should.be.empty;
                await panel.append("Hello World");
               div.parentElement.style.height.should.be.equal("5em");
            });
        
            it("if the length of the content is greater than 5, it sets the height to the content", async() => {
                let div = dom.window.document.querySelector<HTMLDivElement>(`#outputPanel`);
                let content = ["Line 1", "Line 2", "Line 3", "Line 4", "Line 5", "Line 6", "Line 7"];
                let panel = new PreOutputPanel(div);
               div.parentElement.style.height.should.be.empty;
                await panel.append(content);
               div.parentElement.style.height.should.be.equal("7em");
            });

            it("once the height has been set it doesnt change in the next call", async() => {
                let div = dom.window.document.querySelector<HTMLDivElement>(`#outputPanel`);
                let panel = new PreOutputPanel(div);
                let content = ["Line 1", "Line 2"];
                await panel.append(content);
               div.parentElement.style.height.should.be.equal("5em");
                content = ["Line 1", "Line 2", "Line 3", "Line 4", "Line 5", "Line 6"];
                await panel.append(content);
               div.parentElement.style.height.should.be.equal("5em");
            });
        
            it("if the div doesnt have the size-to-content class, it doesnt modify the height", async() => {
                let dom = new JSDOM(
                    `<!DOCTYPE html>
                    <html lang="en">
                    <body>
                        <div id="outputPanel"></div>
                    </body>
                    </html>`,
                    {
                        url: "http://localhost",
                        runScripts: "dangerously"
                    });
                let div = dom.window.document.querySelector<HTMLDivElement>(`#outputPanel`);
                let content = ["Line 1", "Line 2", "Line 3", "Line 4", "Line 5", "Line 6", "Line 7"];
                let panel = new PreOutputPanel(div);
                await panel.append(content);
               div.parentElement.style.height.should.be.empty;
            });
        });
    });

});