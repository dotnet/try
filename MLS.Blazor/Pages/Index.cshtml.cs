// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json.Linq;
using MLS.WasmCodeRunner;

namespace MLS.Blazor.Pages
{
    public class IndexModel : BlazorComponent
    {
        public IndexModel()
        {
        }

        protected override void OnAfterRender()
        {
            base.OnAfterRender();

            PostMessage(JObject.FromObject(new { ready = true }).ToString());
        }

        public static Task<string> PostMessage(string message)
        {
            // Implemented in interop.js
            return JSRuntime.Current.InvokeAsync<string>(
                "BlazorInterop.postMessage",
                message);
        }

        [JSInvokable]
        public static async Task<bool> PostMessageAsync(string message)
        {
            try
            {
                var result = CodeRunner.ProcessRunRequest(message);
                if (result != null)
                {
                    await PostMessage(JObject.FromObject(result).ToString());
                }
            }
            catch
            {
            }

            return true;
        }
    }
}