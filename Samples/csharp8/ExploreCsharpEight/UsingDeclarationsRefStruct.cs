using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ExploreCsharpEight
{
    internal class ResourceHog : IDisposable
    {
        private string name;
        private bool beenDisposed;

        public ResourceHog(string name) => this.name = name;

        public void Dispose()
        {
            beenDisposed = true;
            Console.WriteLine($"Disposing {name}");
        }

        internal void CopyFrom(ResourceHog src)
        {
            switch (beenDisposed, src.beenDisposed)
            {
                case (true, true): throw new ObjectDisposedException($"Resource {name} has already been disposed");
                case (true, false): throw new ObjectDisposedException($"Resource {name} has already been disposed");
                case (false, true): throw new ObjectDisposedException($"Resource {name} has already been disposed");
                default: Console.WriteLine($"Copying from {src.name} to {name}"); return;
            };

        }
    }

    internal class UsingDeclarationsRefStruct
    {
        internal int OldStyle()
        {
            #region Using_Block
            using (var src = new ResourceHog("source"))
            {
                using (var dest = new ResourceHog("destination"))
                {
                    dest.CopyFrom(src);
                }
                Console.WriteLine("After closing destination block");
            }
            Console.WriteLine("After closing source block");
            #endregion
            return 0;
        }
        internal int NewStyle()
        {
            #region Using_Declaration
            using var src = new ResourceHog("source");
            using var dest = new ResourceHog("destination");
            dest.CopyFrom(src);
            Console.WriteLine("Exiting block");
            #endregion
            return 0;
        }
    }
}
