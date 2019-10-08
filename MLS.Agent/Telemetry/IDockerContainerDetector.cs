// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MLS.Agent.Telemetry
{
    public enum IsDockerContainerResult
    {
        True,
        False,
        Unknown
    }

    public interface IDockerContainerDetector
    {
        IsDockerContainerResult IsDockerContainer();
    }
}
