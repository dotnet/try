// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.ObjectModel;

#nullable disable

namespace Microsoft.TryDotNet.PeakyTests;

public class HostOriginPolicies
{
    private static readonly string[] _authorizedPools = { "queue-backed", "storage-backed" };

    private readonly ConcurrentDictionary<Uri, HostOriginPolicy> _policyForHostOriginUri = new();

    static HostOriginPolicies()
    {
        // FIX: (HostOriginPolicies) clean up

        var productionPolicies = new Dictionary<string, HostOriginPolicy>
        {
            // Microsoft
            { "microsoft.com", _authorizedPools, false },
            { "asp.net", _authorizedPools },
            { "dot.net", _authorizedPools },
            { "trydotnet-eastus.azurewebsites.net", _authorizedPools },
            { "trydotnet-westus.azurewebsites.net", _authorizedPools },
            { "trydotnet-monitoring.azurewebsites.net", _authorizedPools },
            { "visualstudio.com", _authorizedPools },
            { "netlandingpage-staging-trydotnet.azurewebsites.net", _authorizedPools },
            { "netlandingpage.azurewebsites.net", _authorizedPools },
            { "netlandingpage-ppe.azurewebsites.net", _authorizedPools },
            { "qadevblogs.wpengine.com", _authorizedPools },

            // Bellevue College
            { "bc.instructure.com", _authorizedPools },
            { "bcconted.instructure.com", _authorizedPools },

            // AI School
            { "standard-learning-paths-dev.azurewebsites.net", _authorizedPools },
            { "standard-learning-paths-test.azurewebsites.net", _authorizedPools },
            { "ai-school-ppe.ai-platforms.p.azurewebsites.net", _authorizedPools },
            { "aischool.microsoft.com", _authorizedPools },
            { "iot-school-dev.azurewebsites.net", _authorizedPools },
            { "iot-school-test.azurewebsites.net", _authorizedPools },

            // nodatime
            { "nodatime.org", _authorizedPools },
            { "test.nodatime.org", _authorizedPools },

            // nuget.org 
            { "nuget.org", _authorizedPools, false },
            { "nugettest.org", _authorizedPools, false },

            //build demo
            { "interactivedotnet.azurewebsites.net", _authorizedPools, false },
            { "trialinteractivedotnet.azurewebsites.net", _authorizedPools, false }
        };

        var localPolicies = new Dictionary<string, HostOriginPolicy>(productionPolicies)
        {
            { "localhost:27261", _authorizedPools },
            { "localhost:58737", _authorizedPools },
            { "localhost:7061", _authorizedPools },
            { "localhost:7062", _authorizedPools },
        };

        ForProduction = productionPolicies.ToReadOnly();
        ForLocal = localPolicies.ToReadOnly();
    }

    public IReadOnlyDictionary<string, HostOriginPolicy> PolicyForHostOrigin { get; }

    public HostOriginPolicies(IReadOnlyDictionary<string, HostOriginPolicy> configurationForHostOrigin)
    {
        PolicyForHostOrigin = new ReadOnlyDictionary<string, HostOriginPolicy>(
            new Dictionary<string, HostOriginPolicy>(configurationForHostOrigin));
    }

    public static IReadOnlyDictionary<string, HostOriginPolicy> ForProduction { get; }

    public static IReadOnlyDictionary<string, HostOriginPolicy> ForLocal { get; }

    public HostOriginPolicy For(Uri hostOrigin)
    {
        return _policyForHostOriginUri.GetOrAdd(
            hostOrigin,
            _ => ParseAndLookUp());

        HostOriginPolicy ParseAndLookUp()
        {
            if (!hostOrigin.TryParseDomain(out var result))
            {
                return null;
            }

            if (result.FifthLevelDomain != null &&
                PolicyForHostOrigin.TryGetValue(result.FifthLevelDomain, out var policy5))
            {
                return policy5;
            }

            if (result.FourthLevelDomain != null &&
                PolicyForHostOrigin.TryGetValue(result.FourthLevelDomain, out var policy4))
            {
                return policy4;
            }

            if (result.ThirdLevelDomain != null &&
                PolicyForHostOrigin.TryGetValue(result.ThirdLevelDomain, out var policy3))
            {
                return policy3;
            }

            if (result.SecondLevelDomain != null &&
                PolicyForHostOrigin.TryGetValue(result.SecondLevelDomain, out var policy2))
            {
                return policy2;
            }

            return null;
        }
    }
}