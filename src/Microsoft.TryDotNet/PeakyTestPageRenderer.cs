// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Web;
using Peaky;

namespace Microsoft.TryDotNet;

public class PeakyTestPageRenderer : ITestPageRenderer
{
    private readonly IServiceProvider _serviceProvider;

    public PeakyTestPageRenderer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task Render(HttpContext context)
    {
        var testDefinitions = _serviceProvider.GetRequiredService<TestDefinitionRegistry>();
        var testTargets = _serviceProvider.GetRequiredService<TestTargetRegistry>();

        await WriteTestList();

        async Task WriteTestList()
        {
            foreach (var target in testTargets)
            {
                await context.Response.WriteAsync(
                    $"""
                     <div>
                         <h2>Tests for environment "{target.Environment}"</h2>
                     """);

                foreach (var test in testDefinitions)
                {
                    if (test.AppliesTo(target))
                    {
                        var urlString = HttpUtility.HtmlAttributeEncode("");

                        await context.Response.WriteAsync(
                            $"""
                             <details>
                                 <summary><a href="{urlString}">{test.TestName}</a></summary>
                                 <div style="display:inline-block;margin-left:3em;">
                                     Application: {target.Application}
                                     <br/>
                                     Environment: {target.Environment}
                                     <br/>
                                     Tags: {string.Join(",", test.Tags)}
                                 </div>
                             </details>
                             """);
                    }
                }

                await context.Response.WriteAsync(
                    """
                    </div>
                    """);
            }
        }
    }
}