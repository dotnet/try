// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace Microsoft.TryDotNet.IntegrationTests;

internal class MessageInterceptor
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> _completionSources = new (StringComparer.OrdinalIgnoreCase);
    public List<JsonElement> Messages { get; } = new();

    public async Task InstallAsync(IPage page)
    {
        await page.ExposeFunctionAsync("postMessageLogger", async (JsonElement message) =>
        {
            await Task.Yield();
            Messages.Add(message);
            if (message.TryGetProperty("type", out var typeProperty))
            {
                var messageType = typeProperty.GetString();
                if (messageType is not null)
                {
                    if (_completionSources.TryRemove(messageType, out var cs))
                    {
                        cs.SetResult(message);
                    }
                }
            }

        });
    }

    public Task<JsonElement> AwaitForMessage(string messageType, TimeSpan? timeOut = null)
    {
        var cs =  _completionSources.GetOrAdd(messageType, _ => new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously));

        if (Debugger.IsAttached)
        {
            return cs.Task;
        }

        return cs.Task.Timeout(timeOut ?? TimeSpan.FromMinutes(1), $"Timeout waiting for message of type {messageType}");
    }
}

