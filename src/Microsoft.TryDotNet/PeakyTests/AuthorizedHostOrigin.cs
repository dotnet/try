// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Primitives;

#nullable disable

namespace Microsoft.TryDotNet.PeakyTests;

public class AuthorizedHostOrigin
{
    public AuthorizedHostOrigin(Uri specifiedDomain, string authorizedDomain, bool enableBranding)
    {
        RequestedUri = specifiedDomain ?? throw new ArgumentNullException(nameof(specifiedDomain));
        AuthorizedDomain = authorizedDomain ?? throw new ArgumentNullException(nameof(authorizedDomain));
        EnableBranding = enableBranding;
    }

    public Uri RequestedUri { get; }

    public string AuthorizedDomain { get; }

    public bool EnableBranding { get; }

    public static AuthorizedHostOrigin FromRequest(
        HttpRequest httpRequest,
        HostOriginPolicies hostOriginPolicies)
    {
        Uri hostOrigin = null;
        HostOriginPolicy policy = null;

        var foundAuthorizedHostOrigin =
            (httpRequest.Query.TryGetValue("hostOrigin", out var fromQueryString) &&
             TryGetHostOrigin(fromQueryString)) ||
            (httpRequest.Headers.TryGetValue("referer", out var fromReferrer) &&
             TryGetHostOrigin(fromReferrer));

        if (foundAuthorizedHostOrigin)
        {
            return new AuthorizedHostOrigin(hostOrigin, policy.HostingDomain, policy.EnableBranding);
        }
        else
        {
            return null;
        }

        bool TryGetHostOrigin(StringValues stringValues)
        {
            if (stringValues.Count != 1
                || !Uri.TryCreate(stringValues.Single(), UriKind.Absolute, out hostOrigin)
                || (policy = hostOriginPolicies.For(hostOrigin)) == null)
            {
                policy = null;
                hostOrigin = null;
                return false;
            }
            else
            {
                hostOrigin = new Uri($"{hostOrigin.Scheme}://{hostOrigin.Authority}");
                return true;
            }
        }
    }
}