// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

using System.Net;
using System.Text;
using FluentAssertions;
using Newtonsoft.Json;
using Peaky;

namespace Microsoft.TryDotNet.PeakyTests;

internal class OutsideInTests : IPeakyTest, IApplyToApplication, IHaveTags
{
    private readonly HttpClientWithTelemetry _httpClient;
    private readonly HttpClientWithXsrfCookies _httpClientWithXsrfCookies;
    private readonly TestTarget _testTarget;

    public OutsideInTests(
        HttpClientWithTelemetry httpClient,
        HttpClientWithXsrfCookies httpClientWithXsrfCookies,
        TestTarget testTarget)
    {
        _httpClient = httpClient;
        _httpClientWithXsrfCookies = httpClientWithXsrfCookies;
        _testTarget = testTarget;
    }

    public string[] Tags => ["deployment"];
    
    public async Task<string> Version_sensor_returns_200()
    {
        var response = await _httpClient.GetAsync("/sensors/version");

        response.ShouldSucceed(HttpStatusCode.OK);

        return await response.Content.ReadAsStringAsync();
    }
        
    public async Task Editor_returns_200()
    {
        var response = await _httpClient.GetAsync("/editor");

        response.ShouldSucceed(HttpStatusCode.OK);
    }

    public async Task<string> IDE_returns_200()
    {
        var response = await _httpClient.GetAsync("/ide");

        response.ShouldSucceed(HttpStatusCode.OK);

        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> A_call_to_the_default_page_returns_200()
    {
        var response = await _httpClient.GetAsync("/");

        response.ShouldSucceed(HttpStatusCode.OK);

        return await response.Content.ReadAsStringAsync();
    }

    public async Task Response_headers_include_Content_Security_Policy()
    {
        var response = await _httpClient.GetAsync("/");

        response.Headers.GetValues("Content-Security-Policy").First().Should().Contain("default-src 'none';");
    }

    public async Task Response_headers_include_X_Content_Type_Options()
    {
        var response = await _httpClient.GetAsync("/");

        response.Headers.GetValues("X-Content-Type-Options").First().Should().Contain("nosniff");
    }

    public async Task<string> Snippet_from_GITHub_with_hostOrigin_and_XSRF_return_200()
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("snippet?from=https://raw.githubusercontent.com/piotrpMSFT/Microsoft.Owin.MockService/master/src/Microsoft.Kestrel.MockService/ConstantMemberEvaluationVisitor.cs&hostOrigin=https%3A%2F%2Ftry.dot.net%2F", UriKind.RelativeOrAbsolute)
        };

        var response = await _httpClientWithXsrfCookies.SendAsync(request);

        response.ShouldSucceed(HttpStatusCode.OK);

        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> Snippet_from_VSTO_and_hostOrigin_and_XSRF_returns_200()
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("snippet?from=https://devdiv.visualstudio.com/DevDiv/MLSDEV/_git/MLS-Orchestrator?path=%2F.gitattributes&hostOrigin=https%3A%2F%2Ftry.dot.net%2F", UriKind.RelativeOrAbsolute)
        };

        var response = await _httpClientWithXsrfCookies.SendAsync(request);

        response.ShouldSucceed(HttpStatusCode.OK);

        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> Snippet_without_hostOrigin_returns_401()
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("snippet?from=https://devdiv.visualstudio.com/DevDiv/MLSDEV/_git/MLS-Orchestrator?path=%2F.gitattributes", UriKind.RelativeOrAbsolute)
        };

        var response = await _httpClientWithXsrfCookies.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "Because hostOrigin is missing");

        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> Snippet_without_Xsrf_returns_401()
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("snippet?from=https://devdiv.visualstudio.com/DevDiv/MLSDEV/_git/MLS-Orchestrator?path=%2F.gitattributes", UriKind.RelativeOrAbsolute)
        };

        var response = await _httpClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "Because XSRF tokens are missing and ASP.NET Core returns 401 for GET");

        return await response.Content.ReadAsStringAsync();
    }
    
    public async Task<object> Compile_without_Xsrf_returns_400()
    {
        var cacheAvoidingValue = Guid.NewGuid();

        var sourceCode =
            $"using System;\n public class Program\n {{\n public static void Main(params object[] args)\n {{\n Console.WriteLine(\"{cacheAvoidingValue}!\");\n }}\n }}\n Program.Main();";

        var workspace = new
        {
            workspaceType = "console",
            buffers = new[]
            {
                new
                {
                    id = "Program.cs",
                    content = sourceCode,
                    position = 0
                }

            }
        };

        var contentPost = new StringContent(JsonConvert.SerializeObject(workspace), Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage
        {
            Content = contentPost,
            Method = HttpMethod.Post,
            RequestUri = new Uri("/workspace/run?hostOrigin=https%3A%2F%2Ftry.dot.net%2F", UriKind.RelativeOrAbsolute)
        };

        var response = await _httpClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "Because XSRF tokens are missing and ASP.NET Core returns 400 for POST");

        return await response.Content.ReadAsStringAsync();
    }

    public async Task<object> Compile_without_hostOrigin_returns_401()
    {
        var cacheAvoidingValue = Guid.NewGuid();

        var sourceCode =
            $"using System;\n public class Program\n {{\n public static void Main(params object[] args)\n {{\n Console.WriteLine(\"{cacheAvoidingValue}!\");\n }}\n }}\n Program.Main();";

        var workspace = new
        {
            workspaceType = "console",
            buffers = new[]
            {
                new
                {
                    id = "Program.cs",
                    content = sourceCode,
                    position = 0
                }

            }
        };

        var contentPost = new StringContent(JsonConvert.SerializeObject(workspace), Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage
        {
            Content = contentPost,
            Method = HttpMethod.Post,
            RequestUri = new Uri("/workspace/run", UriKind.RelativeOrAbsolute)
        };

        var response = await _httpClientWithXsrfCookies.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "Because hostOrigin is missing");

        return await response.Content.ReadAsStringAsync();
    }

    public async Task Requests_with_http_get_redirected_to_https()
    {
        var httpsBaseAddress = _httpClient.BaseAddress;

        var httpBaseAddress = new Uri(httpsBaseAddress.OriginalString.Replace("https", "http"));

        var httpClientHandler = new HttpClientHandler { AllowAutoRedirect = false };

        var httpClient = HttpClientWithTelemetry.Hack_CreateHttpClientWithTelemetryButHideCtorFromPocketContainer(_testTarget, httpClientHandler);

        var response = await httpClient.GetAsync(httpBaseAddress);

        response.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);

        response.Headers.Location.Should().Be(httpsBaseAddress);
    }

    public async Task<string> BundleJs_has_gzip_Content_Encoding()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/client/bundle.js");

        request.Headers.Add("Accept-Encoding", "gzip");

        var response = await _httpClient.SendAsync(request);

        response.ShouldSucceed();

        IEnumerable<string> contentEncodingHeaderValues;

        response.Content.Headers.TryGetValues("Content-Encoding", out contentEncodingHeaderValues)
                .Should().BeTrue("because a Content-Encoding header should be present");

        contentEncodingHeaderValues.Should().Contain("gzip");

        return response.ToString();
    }

    public bool AppliesToApplication(string application)
    {
        return application == "trydotnet";
    }
}