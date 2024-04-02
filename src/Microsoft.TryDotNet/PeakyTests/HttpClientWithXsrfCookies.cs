using Peaky;

namespace Microsoft.TryDotNet.PeakyTests;

public class HttpClientWithXsrfCookies : HttpClientWithTelemetry
{
    public HttpClientWithXsrfCookies(TestTarget testTarget) : base(testTarget, new XsrfCookieHandlingMessageHandler(testTarget.BaseAddress))
    {
    }
}