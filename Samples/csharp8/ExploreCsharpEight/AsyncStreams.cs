using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExploreCsharpEight
{
    class AsyncStreams
    {
        #region AsyncStreams_Declare
        internal async IAsyncEnumerable<int> GenerateSequence()
        {
            for (int i = 0; i < 20; i++)
            {
                // every 3 elements, wait 2 seconds:
                if (i % 3 == 0)
                    await Task.Delay(3000);
                yield return i;
            }
        }
        #endregion

        internal async Task<int> ConsumeStream()
        {
            #region AsyncStreams_Consume
            await foreach (var number in GenerateSequence())
            {
                Console.WriteLine($"The time is {DateTime.Now:hh:mm:ss}. Retrieved {number}");
            }
            #endregion

            return 0;
        }

    }
}
