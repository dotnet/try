// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Microsoft.TryDotNet.PeakyTests;

internal class DomainParseResult
{
    public DomainParseResult(
        string secondLevelDomain = null,
        string thirdLevelDomain = null,
        string fourthLevelDomain = null,
        string fifthLevelDomain = null)
    {
        SecondLevelDomain = secondLevelDomain;
        ThirdLevelDomain = thirdLevelDomain;
        FourthLevelDomain = fourthLevelDomain;
        FifthLevelDomain = fifthLevelDomain;
    }

    public string SecondLevelDomain { get; }
    public string ThirdLevelDomain { get; }
    public string FourthLevelDomain { get; }
    public string FifthLevelDomain { get; }
}