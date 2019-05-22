// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Terminal } from "xterm";
import { fit } from "xterm/lib/addons/fit/fit";
import { webLinksInit } from "xterm/lib/addons/webLinks/webLinks";

import { isString } from "./stringExtensions";
import { ITerminal } from './terminal';

export class XtermTerminal implements ITerminal {
    private xterm: Terminal;

    constructor(private div: HTMLDivElement) {
        if (!this.div) {
            throw new Error("element cannot be null");
        }

        this.xterm = new Terminal({
            cursorBlink: true
        });
    }

    public initialise(): void {
        this.xterm.open(this.div);
        webLinksInit(this.xterm);
        fit(this.xterm);
    }

    public clear(): Promise<void> {
        this.xterm.clear();
        return Promise.resolve();
    }

    public setContent(content: string): Promise<void> {
        return this.write(content);
    }

    public async write(content: string | string[]): Promise<void> {
        await this.clear();
        this.append(content);
    }

    public append(content: string | string[]): Promise<void> {
        if (isString(content)) {
            this.xterm.writeln(content);
        }
        else {
            for (let line of content) {
                this.xterm.writeln(line);
            }
        }
        return Promise.resolve();
    }

    public id(): string {
        return this.div.id;
    }
}
