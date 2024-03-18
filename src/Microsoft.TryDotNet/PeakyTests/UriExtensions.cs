// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

    public static bool TryParseDomainz(
        this Uri subject,
        out string secondLevelDomain,
        out string thirdLevelDomain)
    {
        if (subject == null)
        {
            throw new ArgumentNullException(nameof(subject));
        }

        if (!subject.IsAbsoluteUri)
        {
            secondLevelDomain = null;
            thirdLevelDomain = null;

            return false;
        }

        if (subject.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            secondLevelDomain = $"localhost:{subject.Port}";
            thirdLevelDomain = null;

            return true;
        }

        var hostParts = subject.Host.Split('.');

        if (hostParts.Length < 2)
        {
            secondLevelDomain = null;
            thirdLevelDomain = null;

            return false;
        }

        secondLevelDomain = $"{hostParts[hostParts.Length - 2]}.{hostParts[hostParts.Length - 1]}";

        thirdLevelDomain =
            hostParts.Length == 3
                ? $"{hostParts[hostParts.Length - 3]}.{secondLevelDomain}"
                : null;

        return true;
    }
}