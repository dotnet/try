// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { test, expect } from '@playwright/test';
import * as cp from 'child_process';
import * as messages from '../src/messages';
import { dispatchApiMessage, typeText } from './playwrightTestSupport';

let p: cp.ChildProcessWithoutNullStreams | undefined;

test.use({
    //headless: false,
    video: {
        mode: 'on'
    },

});
test.beforeAll(async () => {
    p = cp.spawn('cmd.exe', ['/c', 'npx', 'http-server']);
});

test('can load monaco editor', async ({ page }) => {
    await page.goto('http://localhost:8080/dist/index.html');
    await page.waitForLoadState('networkidle');
    const editor = page.locator('div[role = "code"]');
    await expect(editor).toBeVisible();

});

test('can load wasm-runner', async ({ page }) => {
    await page.goto('http://localhost:8080/dist/index.html');
    await page.waitForLoadState('networkidle');
    const runner = page.locator('[role = "wasm-runner"]');
    await expect(runner).not.toBeUndefined();

    await expect(runner).toHaveAttribute("aria-hidden", "true");

});

test('can configure monaco editor theme', async ({ page }) => {
    await page.goto('http://localhost:8080/dist/index.html');
    await page.waitForLoadState('networkidle');

    await dispatchApiMessage(page, {
        type: messages.CONFIGURE_MONACO_REQUEST,
        theme: "vs-dark"
    });
    const editor = page.locator('div[role = "code"]');
    await expect(editor).toHaveClass(/\.*vs-dark\.*/);

});

test('can configure monaco editor options', async ({ page }) => {
    await page.goto('http://localhost:8080/dist/index.html');
    await page.waitForLoadState('networkidle');

    let minimap = page.locator('div.minimap');
    await expect(minimap).toBeVisible();

    await dispatchApiMessage(page, {
        type: messages.CONFIGURE_MONACO_REQUEST,
        editorOptions: {
            minimap: {
                enabled: false
            }
        }
    });

    minimap = page.locator('div.minimap');
    await expect(minimap).not.toBeVisible();
});

test("can set monaco editor buffer", async ({ page }) => {
    await page.goto('http://localhost:8080/dist/index.html');
    await page.waitForLoadState('networkidle');

    const inputArea = page.locator('div.editor-scrollable');

    await dispatchApiMessage(page, {
        type: messages.SET_EDITOR_CODE_REQUEST,
        requestId: "request-1",
        sourceCode: "await Task.Delay(200);"
    });

    await expect(inputArea).toHaveText("await Task.Delay(200);");

});

test("user typing code gets diagnostics ", async ({ page, context }) => {
    await page.goto('http://localhost:8080/dist/index.html');
    await page.waitForLoadState('networkidle');

    await dispatchApiMessage(page, {
        type: messages.SET_EDITOR_CODE_REQUEST,
        requestId: "request-1",
        sourceCode: ""
    });

    let requestData: any = {};
    await context.addInitScript(() => delete ((<any>window.navigator).serviceWorker));
    await context.route("**/*", route => {
        requestData = route.request().postDataJSON();
        route.fulfill({ status: 200 });
    });

    await typeText(page, "await Task.Delay(\"Error\");", { delay: 100 });

    const inputArea = page.locator('div.editor-scrollable');

    await expect(inputArea).toHaveText("await Task.Delay(\"Error\");");

});

// todo : how to test theme being sete
// test("can define custom monaco editor theme", async ({ page }) => {
//     await page.goto('http://localhost:8080/dist/index.html');
//     await page.waitForLoadState('networkidle');


//     await dispatchApiMessage(page, {
//         type: messages.DEFINE_THEMES_REQUEST,
//         themes: {
//             myCustomTheme: {
//                 base: "vs-dark",
//                 colors: {},
//                 inherit: true,
//                 rules: [
//                     { token: "comment", foreground: "ffa500", fontStyle: "italic underline" },
//                     { token: "comment.js", foreground: "008800", fontStyle: "bold" },
//                     { token: "comment.css", foreground: "0000ff" }
//                 ]
//             }
//         }
//     });

//     await dispatchApiMessage(page, {
//         type: messages.CONFIGURE_MONACO_REQUEST,
//         theme: "myCustomTheme"
//     });

//     const editor = await page.locator('div[role = "code"]');
//     await expect(editor).toHaveClass(/\.*vs\.*/);
// });

test.afterEach(async ({ page }) => {
    console.log(await page.video().path());
});

test.afterAll(async () => {
    p?.kill('SIGTERM');
});