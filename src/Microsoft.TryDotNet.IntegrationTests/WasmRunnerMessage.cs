// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.TryDotNet.IntegrationTests;

internal record WasmRunnerMessage(string type, string? message = null, SerializableCodeRunnerResult? result = null) { }