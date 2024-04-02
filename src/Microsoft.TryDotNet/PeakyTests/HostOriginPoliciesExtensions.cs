// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

using System.Collections.ObjectModel;

namespace Microsoft.TryDotNet.PeakyTests;

internal static class HostOriginPoliciesExtensions
{
    public static IReadOnlyDictionary<K, V> ToReadOnly<K, V>(this IDictionary<K, V> subject)
    {
        subject = subject ?? throw new ArgumentNullException(nameof(subject));

        return new ReadOnlyDictionary<K, V>(subject);
    }

    public static IDictionary<string, HostOriginPolicy> Add(
        this IDictionary<string, HostOriginPolicy> dictionary,
        string domain,
        string[] authorizedPools,
        bool enableBranding = true)
    {
        dictionary.Add(domain,
                       new HostOriginPolicy(domain,
                                            authorizedPools,
                                            enableBranding,
                                            maxAgentReassignmentsPerRequest: 1));
        return dictionary;
    }
}