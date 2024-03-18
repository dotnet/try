using System.Net;
using FluentAssertions;

namespace Microsoft.TryDotNet.PeakyTests;

public class XsrfCookieHandlingMessageHandler : HttpClientHandler
{
    private readonly Uri _baseAddress;
    private Cookie? _xsrfCookie;

    public XsrfCookieHandlingMessageHandler(Uri baseAddress) 
    {
        _baseAddress = baseAddress;
        CookieContainer = new CookieContainer();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_xsrfCookie is null)
        {
            var cookieRequest = new HttpRequestMessage(HttpMethod.Get, _baseAddress);

            await base.SendAsync(cookieRequest, cancellationToken);
        }

        var cookies = CookieContainer.GetCookies(_baseAddress).ToArray();

        cookies.Should().Contain(c => c.Name == "XSRF-TOKEN");

        _xsrfCookie = cookies.First(c => c.Name == "XSRF-TOKEN");

        request.Headers.Add("xsrf-token", _xsrfCookie.Value);

        return await base.SendAsync(request, cancellationToken);
    }
}