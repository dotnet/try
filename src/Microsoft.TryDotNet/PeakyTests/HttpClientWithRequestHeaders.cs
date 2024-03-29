using Peaky;

namespace Microsoft.TryDotNet.PeakyTests;

public class HttpClientWithRequestHeaders : HttpClient
{
    public HttpClientWithRequestHeaders(TestTarget testTarget) : base(new XsrfCookieHandlingMessageHandler(testTarget.BaseAddress), false)
    {
        BaseAddress = testTarget?.BaseAddress ??
                      throw new ArgumentNullException(nameof(testTarget));
    }
}