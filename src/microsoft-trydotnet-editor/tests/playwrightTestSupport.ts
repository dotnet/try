// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as test from "@playwright/test";
import * as messages from "../src/messages";

export async function dispatchApiMessage(page: test.Page, apiMessage: messages.AnyApiMessage): Promise<void> {
    await page.evaluate((eventData) => window.dispatchEvent(new MessageEvent('message', {
        data: eventData
    })), apiMessage);
}

export async function waitFoEditor(page: test.Page) {
    const editor = await page.locator('div[role = "code"]');
    editor.waitFor({ state: 'visible' });

}

export async function typeText(page: test.Page, text: string, options?: { delay?: number; noWaitAfter?: boolean; timeout?: number; }): Promise<void> {
    const editor = page.locator('textarea[role = "textbox"]');

    await editor.focus();
    await editor.type(text, options);
}
