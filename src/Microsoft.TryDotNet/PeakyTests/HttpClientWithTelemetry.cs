using System.Diagnostics;
using Peaky;

namespace Microsoft.TryDotNet.PeakyTests;

public class HttpClientWithTelemetry : HttpClient
{
    private readonly string _baseRequestId = $"Peaky-{Guid.NewGuid()}";
    private int _requestSequenceNumber;

    public HttpClientWithTelemetry(TestTarget testTarget)
    {
        BaseAddress = testTarget?.BaseAddress ??
                      throw new ArgumentNullException(nameof(testTarget));
    }

    internal HttpClientWithTelemetry(
        TestTarget testTarget,
        HttpMessageHandler messageHandler) : base(messageHandler, false)
    {
        BaseAddress = testTarget?.BaseAddress ??
                      throw new ArgumentNullException(nameof(testTarget));
    }

    public static HttpClientWithTelemetry Hack_CreateHttpClientWithTelemetryButHideCtorFromPocketContainer(TestTarget testTarget, HttpMessageHandler messageHandler)
    {
        return new HttpClientWithTelemetry(testTarget, messageHandler);
    }

    public new Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        var requestId = GetNextRequestId();

        request.Headers.Add("Request-Id", requestId);

        Trace.WriteLine($"Request-Id: {requestId}");

        return base.SendAsync(request);
    }

    public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var requestId = GetNextRequestId();

        request.Headers.Add("Request-Id", requestId);

        Trace.WriteLine($"Request-Id: {requestId}");

        return base.SendAsync(request, cancellationToken);
    }

    private string GetNextRequestId() => $"{_baseRequestId}-{Interlocked.Increment(ref _requestSequenceNumber)}";
}