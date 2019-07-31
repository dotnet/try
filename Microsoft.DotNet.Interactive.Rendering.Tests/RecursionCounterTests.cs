// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.Interactive.Rendering.Tests
{
    public class RecursionCounterTests
    {
        [Fact]
        public async Task RecursionCounter_does_not_share_state_across_threads()
        {
            var participantCount = 3;
            var barrier = new Barrier(participantCount);
            var counter = new RecursionCounter();

            var tasks = Enumerable.Range(1, participantCount)
                                  .Select(i =>
                                  {
                                      return Task.Run(() =>
                                      {
                                          barrier.SignalAndWait();

                                          counter.Depth.Should().Be(0);

                                          using (counter.Enter())
                                          {
                                              barrier.SignalAndWait();

                                              counter.Depth.Should().Be(1);

                                              using (counter.Enter())
                                              {
                                                  counter.Depth.Should().Be(2);
                                              }
                                          }

                                          counter.Depth.Should().Be(0);
                                      });
                                  });

            await Task.WhenAll(tasks);
        }
    }
}