using System;
using System.Collections.Generic;
using System.Text;

namespace ExploreCsharpEight
{
    class StaticLocalFunctions
    {
        #region LocalFunction_Counting
        public IEnumerable<int> Counter(int start, int end)
        {
            if (start >= end) throw new ArgumentOutOfRangeException("start must be less than end");

            return localCounter();

            IEnumerable<int> localCounter()
            {
                for (int i = start; i < end; i++)
                    yield return i;
            }
        }
        #endregion

        internal int LocalFunctionWithCapture()
        {
            var items = Counter(1, 10);
            foreach(var item in items)
            {
                Console.WriteLine(item);
            }
            return 1;
        }

        internal int LocalFunctionWithNoCapture()
        {
            var items = StaticCounter(1, 10);
            foreach (var item in items)
            {
                Console.WriteLine(item);
            }
            return 1;
        }


        #region LocalFunction_Static
        public IEnumerable<int> StaticCounter(int start, int end)
        {
            if (start >= end) throw new ArgumentOutOfRangeException("start must be less than end");

            return localCounter(start, end);

            static IEnumerable<int> localCounter(int first, int endLocation)
            {
                for (int i = first; i < endLocation; i++)
                    yield return i;
            }
        }
        #endregion

    }
}
