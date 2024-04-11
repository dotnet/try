// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.TryDotNet.IntegrationTests;

[CollectionDefinition(nameof(IntegratedServicesFixture), DisableParallelization = true)]
public class CollectionDefinitionForIntegratedServicesFixture : ICollectionFixture<IntegratedServicesFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}