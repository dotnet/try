// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TryDotNet.PeakyTests;

internal static class HostOriginAuthenticationHandlerExtensions
{
    public static void AddHostOriginAuth(this IServiceCollection services, HostOriginPolicies policies)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (policies == null)
        {
            throw new ArgumentNullException(nameof(policies));
        }

        services.AddSingleton(policies);

        var authenticationBuilder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "HostOrigin";
            options.DefaultChallengeScheme = "HostOrigin";
        });

        authenticationBuilder.AddScheme<HostOriginAuthenticationOptions, HostOriginAuthenticationHandler>(
            "HostOrigin",
            "HostOriginAuth",
            _ => { });

        services.AddAuthorization(
            options => options.AddPolicy("HostOrigin",
                                         policy => policy.RequireClaim("HostOriginDomain")));
    }
}