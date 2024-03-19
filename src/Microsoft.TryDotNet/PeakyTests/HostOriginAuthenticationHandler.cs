// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Pocket;
using static Pocket.Logger<Microsoft.TryDotNet.PeakyTests.HostOriginAuthenticationHandler>;

namespace Microsoft.TryDotNet.PeakyTests;

public class HostOriginAuthenticationHandler : AuthenticationHandler<HostOriginAuthenticationOptions>
{
    private readonly HostOriginPolicies _policies;

    public HostOriginAuthenticationHandler(
        IOptionsMonitor<HostOriginAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        HostOriginPolicies configuration) : base(options, logger, encoder)
    {
        _policies = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var hostOrigin = AuthorizedHostOrigin.FromRequest(Request, _policies);

        if (hostOrigin != null)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim("HostOrigin", hostOrigin.RequestedUri.ToString()),
                new Claim("HostOriginDomain", hostOrigin.AuthorizedDomain)
            });

            Log.Event("HostOriginAuthenticationHandler.HostOriginDomain", ("HostOrigin", hostOrigin.AuthorizedDomain));

            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), "HostOrigin");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        else
        {
            Log.Event("HostOriginAuthenticationHandler.HostOriginDomain", ("HostOrigin", "none"));

            return Task.FromResult(AuthenticateResult.NoResult());
        }
    }
}