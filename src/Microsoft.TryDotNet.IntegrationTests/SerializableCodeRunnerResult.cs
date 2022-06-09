// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.TryDotNet.IntegrationTests;

internal record SerializableCodeRunnerResult(bool success, string? error, string? runnerError);