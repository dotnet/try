using System.Text.Json;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.TryDotNet;

public static class EnvelopeExtensions
{
    public static JsonElement ToJsonElement(this IKernelEventEnvelope envelope)
    {
        return JsonDocument.Parse(KernelEventEnvelope.Serialize(envelope)).RootElement;
    }

    public static JsonElement ToJsonElement(this IKernelCommandEnvelope envelope)
    {
        return JsonDocument.Parse(KernelCommandEnvelope.Serialize(envelope)).RootElement;
    }

    public static IEnumerable<JsonElement> ToJsonElements(this IEnumerable<IKernelEventEnvelope> envelopes)
    {
        return envelopes.Select(e => e.ToJsonElement());
    }

    public static IEnumerable<JsonElement> ToJsonElements(this IEnumerable<IKernelCommandEnvelope> envelopes)
    {
        return envelopes.Select(e => e.ToJsonElement());
    }
}