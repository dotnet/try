// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";
import { extractIncludes } from "../src/domInjection/includesProcessor";
import { JSDOM } from "jsdom";
import { expect } from "chai";
import { tryDotNetModes } from "../src/domInjection/types";

chai.should();

describe("include blocks", () => {
  it("can be extracted from dom", () => {
    let configuration = {
        hostOrigin: "https://docs.microsoft.com"
    };
    let dom = new JSDOM(
        `<!DOCTYPE html>
        <html lang="en">
        <body>
            <pre>
                <code data-trydotnet-mode="include">
public class Utility{
    
}
                <code>
            </pre>
            <pre>
                <code data-trydotnet-mode="include">
public class AnotherUtility{
    
}
                <code>
            </pre>
            <pre>
            <code data-trydotnet-mode="include" data-trydotnet-session-id="sessionInclude">
public class AnotherUtilityWhyNot{

}
            <code>
        </pre>
        </body>
        </html>`,
        {
            url: configuration.hostOrigin,
            runScripts: "dangerously"
        });
        
        let includes = extractIncludes(dom.window.document);
        expect(includes).not.to.be.null;
        expect(includes).not.to.be.undefined;
        expect(includes.global).not.to.be.undefined;
        expect(includes.global).not.to.be.null;
        let file = includes.global.files[0];
        expect(file).not.to.be.null;

        expect(includes["sessionInclude"]).not.to.be.undefined;
        expect(includes["sessionInclude"]).not.to.be.null;
        file = includes["sessionInclude"].files[0];
        expect(file).not.to.be.null;
        
  });

  it("can declare documents", () => {
    let configuration = {
        hostOrigin: "https://docs.microsoft.com"
    };
    let dom = new JSDOM(
        `<!DOCTYPE html>
        <html lang="en">
        <body>
            <pre>
                <code data-trydotnet-mode="include"  data-trydotnet-region="region1">
public class Utility{
    
}
                <code>
            </pre>
            <pre>
                <code data-trydotnet-mode="include" data-trydotnet-region="region2">
public class AnotherUtility{
    
}
                <code>
            </pre>
            <pre>
                <code data-trydotnet-mode="include" data-trydotnet-region="region2">
public class AnotherUtilityAgain{
    
}
                <code>
            </pre>          
        </body>
        </html>`,
        {
            url: configuration.hostOrigin,
            runScripts: "dangerously"
        });
        
        let includes = extractIncludes(dom.window.document);
        expect(includes).not.to.be.null;
        expect(includes).not.to.be.undefined;
        expect(includes.global).not.to.be.undefined;
        expect(includes.global).not.to.be.null;
        let file = includes.global.files[0];
        expect(file).not.to.be.null;
        
        includes.global.documents.length.should.be.equal(2);
        let documentOne = includes.global.documents[0];
        expect(documentOne).not.to.be.null;
        documentOne.region.should.be.contain("region1");

        let documentTwo = includes.global.documents[1];
        expect(documentTwo).not.to.be.null;
        documentTwo.region.should.be.contain("region2");

        documentOne.fileName.should.be.equal(documentTwo.fileName);
  });

  it("can concatenate code snippets targeting a single region", () => {
    let configuration = {
        hostOrigin: "https://docs.microsoft.com"
    };
    let dom = new JSDOM(
        `<!DOCTYPE html>
        <html lang="en">
        <body>
            <pre>
                <code data-trydotnet-mode="include"  data-trydotnet-region="region1">
public class Utility{
    
}
                <code>
            </pre>
            <pre>
                <code data-trydotnet-mode="include" data-trydotnet-region="region2" data-trydotnet-injection-point="before">
public class AnotherUtility{
    
}
                <code>
            </pre>
            <pre>
                <code data-trydotnet-mode="include" data-trydotnet-region="region2" data-trydotnet-injection-point="after">
public class AnotherUtilityAgain{
    
}
                <code>
            </pre>          
        </body>
        </html>`,
        {
            url: configuration.hostOrigin,
            runScripts: "dangerously"
        });
        
        let includes = extractIncludes(dom.window.document);
        expect(includes).not.to.be.null;
        expect(includes).not.to.be.undefined;
        expect(includes.global).not.to.be.undefined;
        expect(includes.global).not.to.be.null;
        let file = includes.global.files[0];
        expect(file).not.to.be.null;
        
        includes.global.documents.length.should.be.equal(3);
        let documentOne = includes.global.documents[0];
        expect(documentOne).not.to.be.null;
        documentOne.region.should.be.contain("region1");

        let documentTwo = includes.global.documents[1];
        expect(documentTwo).not.to.be.null;
        documentTwo.region.should.be.equal("region2[before]");

        let documentThree = includes.global.documents[2];
        expect(documentThree).not.to.be.null;
        documentThree.region.should.be.equal("region2[after]");

        documentOne.fileName.should.be.equal(documentTwo.fileName);
  });

  it("can declare documents wrapping around editable one", () => {
    let configuration = {
        hostOrigin: "https://docs.microsoft.com"
    };
    let dom = new JSDOM(
        `<!DOCTYPE html>
        <html lang="en">
        <body>            
            <pre>
                <code data-trydotnet-mode="include" data-trydotnet-region="region2">
public class ClassBefore{
    
}
                <code>
            </pre>
            <pre>
                <code data-trydotnet-mode="editor" data-trydotnet-region="region2">
public class EditableCode{
    
}
                <code>
            </pre>
            <pre>
                <code data-trydotnet-mode="include" data-trydotnet-region="region2">
public class ClassAfter{
    
}
                <code>
            </pre>  
            
            <pre>
            <code data-trydotnet-mode="include" data-trydotnet-region="region2">
public class AnotherClassAfter{

}
            <code>
        </pre>  
        </body>
        </html>`,
        {
            url: configuration.hostOrigin,
            runScripts: "dangerously"
        });
        
        let includes = extractIncludes(dom.window.document);
        expect(includes).not.to.be.null;
        expect(includes).not.to.be.undefined;
        expect(includes.global).not.to.be.undefined;
        expect(includes.global).not.to.be.null;
        let file = includes.global.files[0];
        expect(file).not.to.be.null;
        
        includes.global.documents.length.should.be.equal(2);
        let documentOne = includes.global.documents[0];
        expect(documentOne).not.to.be.null;
        documentOne.region.should.be.contain("region2[before]");

        let documentTwo = includes.global.documents[1];
        expect(documentTwo).not.to.be.null;
        documentTwo.region.should.be.equal("region2[after]");    

        documentOne.fileName.should.be.equal(documentTwo.fileName);
  });

  it("can declare documents wrapping around editable one following the order attribute", () => {
    let configuration = {
        hostOrigin: "https://docs.microsoft.com"
    };
    let dom = new JSDOM(
        `<!DOCTYPE html>
        <html lang="en">
        <body>            
            <pre>
                <code data-trydotnet-mode="include" data-trydotnet-region="region2" data-trydotnet-order="158">
public class ClassBefore{
    
}
                <code>
            </pre>
            <div>
            <div>
            <pre>
                <code data-trydotnet-mode="editor" data-trydotnet-region="region2" data-trydotnet-order="161">
public class EditableCode{
    
}
                <code>
            </pre>
            </div>
            </div>
            <pre>
                <code data-trydotnet-mode="include" data-trydotnet-region="region2" data-trydotnet-order="159">
public class ClassAfter{
    
}
                <code>
            </pre>  
            
            <pre>
            <code data-trydotnet-mode="include" data-trydotnet-region="region2" data-trydotnet-order="160">
public class AnotherClassAfter{

}
            <code>
        </pre>  
        </body>
        </html>`,
        {
            url: configuration.hostOrigin,
            runScripts: "dangerously"
        });
        
        let includes = extractIncludes(dom.window.document);
        expect(includes).not.to.be.null;
        expect(includes).not.to.be.undefined;
        expect(includes.global).not.to.be.undefined;
        expect(includes.global).not.to.be.null;
        let file = includes.global.files[0];
        expect(file).not.to.be.null;
        
        includes.global.documents.length.should.be.equal(1);
        let documentOne = includes.global.documents[0];
        expect(documentOne).not.to.be.null;
        documentOne.region.should.be.contain("region2[before]");
  });

  it("remmoves dom elements that are hidden", () => {
    let configuration = {
        hostOrigin: "https://docs.microsoft.com"
    };
    let dom = new JSDOM(
        `<!DOCTYPE html>
        <html lang="en">
        <body>
            <pre>
                <code data-trydotnet-mode="include" data-trydotnet-visibility="hidden">
public class Utility{
    
}
                <code>
            </pre>
            <pre>
                <code data-trydotnet-mode="include" data-trydotnet-visibility="hidden">
public class AnotherUtility{
    
}
                <code>
            </pre>
            <pre>
            <code data-trydotnet-mode="include" data-trydotnet-session-id="sessionInclude", data-trydotnet-visibility="hidden">
public class AnotherUtilityWhyNot{

}
            <code>
        </pre>
        </body>
        </html>`,
        {
            url: configuration.hostOrigin,
            runScripts: "dangerously"
        });
        
        let includes = extractIncludes(dom.window.document);
        expect(includes).not.to.be.null;       
        
        let includeElements = dom.window.document.querySelectorAll(
            `pre>code[data-trydotnet-mode=${
                tryDotNetModes[tryDotNetModes.include]
            }]`
        );

        includeElements.length.should.be.equal(0);
  });
});
