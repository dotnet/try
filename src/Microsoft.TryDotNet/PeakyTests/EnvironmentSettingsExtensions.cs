// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.TryDotNet.PeakyTests;

internal static class EnvironmentSettingsExtensions
{
    public static IServiceCollection AddEnvironmentSettings(this IServiceCollection services, EnvironmentSettings environmentSettings)
    {
        services = services ??
                   throw new ArgumentNullException(nameof(services));
        environmentSettings = environmentSettings ??
                              throw new ArgumentNullException(nameof(environmentSettings));
        services.TryAddSingleton(environmentSettings);
        return services;
    }

    public static IServiceCollection AddLocalEnvironmentSettings(this IServiceCollection services, out EnvironmentSettings environmentSettings)
    {
        environmentSettings = EnvironmentSettings.ForLocal;
        return services.AddEnvironmentSettings(environmentSettings);
    }

    public static IServiceCollection AddProductionEnvironmentSettings(this IServiceCollection services, out EnvironmentSettings environmentSettings)
    {
        environmentSettings = EnvironmentSettings.ForProduction;
        return services.AddEnvironmentSettings(environmentSettings);
    }
}