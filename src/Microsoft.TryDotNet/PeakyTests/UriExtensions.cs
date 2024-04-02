// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Microsoft.TryDotNet.PeakyTests;

internal static class UriExtensions
{
    public static bool TryParseDomain(
        this Uri subject,
        out DomainParseResult result)
    {
        if (subject == null)
        {
            throw new ArgumentNullException(nameof(subject));
        }

        if (!subject.IsAbsoluteUri)
        {
            result = null;
            return false;
        }

        if (subject.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            result = new DomainParseResult($"localhost:{subject.Port}");

            return true;
        }

        var hostParts = subject.Host.Split('.');

        if (hostParts.Length < 2)
        {
            result = null;

            return false;
        }

        var secondLevelDomain = $"{hostParts[hostParts.Length - 2]}.{hostParts[hostParts.Length - 1]}";

        var thirdLevelDomain =
            hostParts.Length > 2
                ? $"{hostParts[hostParts.Length - 3]}.{secondLevelDomain}"
                : null;

        var fourthLevelDomain =
            hostParts.Length > 3
                ? $"{hostParts[hostParts.Length - 4]}.{thirdLevelDomain}"
                : null;

        var fifthLevelDomain =
            hostParts.Length > 4
                ? $"{hostParts[hostParts.Length - 5]}.{fourthLevelDomain}"
                : null;

        result = new DomainParseResult(
            secondLevelDomain,
            thirdLevelDomain,
            fourthLevelDomain,
            fifthLevelDomain
        );

        return true;
    }
}

