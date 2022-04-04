// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IOutputPanel } from "./outputPanel";
import { htmlEncode, isString, isNullOrUndefinedOrWhitespace } from "./stringExtensions";

export class PreOutputPanel implements IOutputPanel {

    private encode: (input: string) => string;
    private _codeElement: HTMLElement;
    private _id: string;
    private _shouldSizeToContent: boolean;

    constructor(private _div: HTMLDivElement) {
        if (!this._div) {
            throw new Error("element cannot be null");
        }

        this._id = this._div.id;

        let ownerDocument = this._div.ownerDocument;
        let preElement = ownerDocument.createElement("pre");
        this._codeElement = preElement.appendChild(ownerDocument.createElement("code"));
        this._div.appendChild(preElement);
        this._shouldSizeToContent = this._div.classList.contains("size-to-content") || this._div.parentElement.classList.contains("size-to-content");
        this.encode = (text: string): string => {
            return htmlEncode(text, ownerDocument);
        }
    }

    public initialise(): void {

    }

    public write(content: string | string[]): Promise<void> {
        if (isString(content)) {
            this._codeElement.innerHTML = `${this.encode(content)}`;
        }
        else if (content.length > 0) {
            this._codeElement.innerHTML = content.map(line => `${this.encode(line)}`).join("\n");
        }

        this.setHeight(content);
        return Promise.resolve();
    }

    private setHeight(content: string[] | string) {
        if (this._shouldSizeToContent && isNullOrUndefinedOrWhitespace(this._div.parentElement.style.height)) {
            let height: number;
            if (isString(content)) {
                let lineCount = content.split('\n').length;
                height = Math.max(5, lineCount);
            }
            else {
                height = Math.max(5, content.length);
            }
            this._div.parentElement.style.height = `${height}em`;
            this._div.parentElement.style.overflow = "hidden";
            setTimeout(() => {
                this._div.parentElement.style.overflow = "auto";
            }, 600);
        }
    }

    public clear(): Promise<void> {
        this._codeElement.innerHTML = "";
        return Promise.resolve();
    }

    public setContent(content: string): Promise<void> {
        return this.write(content);
    }

    public append(content: string | string[]): Promise<void> {
        if (isString(content)) {
            this._codeElement.innerHTML += this.encode(content);
        }
        else {
            this._codeElement.innerHTML += content.map(line => this.encode(line)).join("\n");
        }

        this.setHeight(content);
        return Promise.resolve();
    }

    public id(): string {
        return this._id;
    }
}
