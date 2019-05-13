// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace test_example
{
    public class UnitTest1
    {
#region test1
        [Fact]
        public void Test1()
        {
            Assert.Equal(ClassBeingTested.GetValue(), 42);
        }
#endregion
    }
}
