// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace WorkspaceServer.Servers.Roslyn
{
    internal class TrackingStringWriter : StringWriter
    {
        private class Region
        {
            public int Start { get; set; }
            public int Length { get; set; }
        }
         List<Region> _regions = new List<Region>();
         private bool _trakingSize;


         public bool WriteOccurred { get; set; }

        public override void Write(char value)
        {
            TrackSize(() => base.Write(value));
        }

        private void TrackSize(Action action)
        {
            WriteOccurred = true;
            if (_trakingSize)
            {
                action();
                return;
            }

            _trakingSize = true;
            var sb = base.GetStringBuilder();

            var region = new Region
            {
                Start = sb.Length
            };

            _regions.Add(region);
            action();
            region.Length = sb.Length - region.Start;
            _trakingSize = false;
        }
        private async Task TrackSizeAsync(Func<Task> action)
        {
            WriteOccurred = true;
            if (_trakingSize)
            {
                await action();
                return;
            }
            _trakingSize = true;
            var sb = base.GetStringBuilder();

            var region = new Region
            {
                Start = sb.Length
            };

            _regions.Add(region);

            await action();
            region.Length = sb.Length - region.Start;
            _trakingSize = false;
        }

        public override void Write(char[] buffer, int index, int count)
        {
            TrackSize(() => base.Write(buffer, index, count));
        }

        public override void Write(string value)
        {
            TrackSize(() => base.Write(value));
        }

        public override Task WriteAsync(char value)
        {
            return TrackSizeAsync(() => base.WriteAsync(value));
        }

        public override Task WriteAsync(char[] buffer, int index, int count)
        {
            return TrackSizeAsync(() => base.WriteAsync(buffer, index, count));
        }

        public override Task WriteAsync(string value)
        {
            return TrackSizeAsync(() => base.WriteAsync(value));
        }

        public override Task WriteLineAsync(char value)
        {
            return TrackSizeAsync(() => base.WriteLineAsync(value));
        }

        public override Task WriteLineAsync(char[] buffer, int index, int count)
        {
            return TrackSizeAsync(() => base.WriteLineAsync(buffer, index, count));
        }

        public override Task WriteLineAsync(string value)
        {
            return TrackSizeAsync(() => base.WriteLineAsync(value));
        }

        public override void Write(bool value)
        {
            TrackSize(() => base.Write(value));
        }

        public override void Write(char[] buffer)
        {
            TrackSize(() => base.Write(buffer));
        }

        public override void Write(decimal value)
        {
            TrackSize(() => base.Write(value));
        }

        public override void Write(double value)
        {
            TrackSize(() => base.Write(value));
        }

        public override void Write(int value)
        {
            TrackSize(() => base.Write(value));
        }

        public override void Write(long value)
        {
            TrackSize(() => base.Write(value));
        }

        public override void Write(object value)
        {
            TrackSize(() => base.Write(value));
        }

        public override void Write(float value)
        {
            TrackSize(() => base.Write(value));
        }

        public override void Write(string format, object arg0)
        {
            TrackSize(() => base.Write(format, arg0));
        }

        public override void Write(string format, object arg0, object arg1)
        {
            TrackSize(() => base.Write(format, arg0, arg1));
        }

        public override void Write(string format, object arg0, object arg1, object arg2)
        {
            TrackSize(() => base.Write(format, arg0, arg1, arg2));
        }

        public override void Write(string format, params object[] arg)
        {
            TrackSize(() => base.Write(format, arg));
        }

        public override void Write(uint value)
        {
            TrackSize(() => base.Write(value));
        }

        public override void Write(ulong value)
        {
            TrackSize(() => base.Write(value));
        }

        public override void WriteLine()
        {
            TrackSize(() => base.WriteLine());
        }

        public override void WriteLine(bool value)
        {
            TrackSize(() => base.WriteLine(value));
        }

        public override void WriteLine(char value)
        {
            TrackSize(() => base.WriteLine(value));
        }

        public override void WriteLine(char[] buffer)
        {
            TrackSize(() => base.WriteLine(buffer));
        }

        public override void WriteLine(char[] buffer, int index, int count)
        {
            TrackSize(() => base.WriteLine(buffer, index, count));
        }

        public override void WriteLine(decimal value)
        {
            TrackSize(() => base.WriteLine(value));
        }

        public override void WriteLine(double value)
        {
            TrackSize(() => base.WriteLine(value));
        }

        public override void WriteLine(int value)
        {
            TrackSize(() => base.WriteLine(value));
        }

        public override void WriteLine(long value)
        {
            TrackSize(() => base.WriteLine(value));
        }

        public override void WriteLine(object value)
        {
            TrackSize(() => base.WriteLine(value));
        }

        public override void WriteLine(float value)
        {
            TrackSize(() => base.WriteLine(value));
        }

        public override void WriteLine(string value)
        {
            TrackSize(() => base.WriteLine(value));
        }

        public override void WriteLine(string format, object arg0)
        {
            TrackSize(() => base.WriteLine(format, arg0));
        }

        public override void WriteLine(string format, object arg0, object arg1)
        {
            TrackSize(() => base.WriteLine(format, arg0, arg1));
        }

        public override void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            TrackSize(() => base.WriteLine(format, arg0, arg1, arg2));
        }

        public override void WriteLine(string format, params object[] arg)
        {
            TrackSize(() => base.WriteLine(format, arg));
        }

        public override void WriteLine(uint value)
        {
            TrackSize(() => base.WriteLine(value));
        }

        public override void WriteLine(ulong value)
        {
            TrackSize(() => base.WriteLine(value));
        }

        public override Task WriteLineAsync()
        {
            return TrackSizeAsync(() => base.WriteLineAsync());
        }

        public IEnumerable<string> Writes()
        {
            var src = base.GetStringBuilder().ToString();
            foreach (var region in _regions)
            {
                yield return src.Substring(region.Start, region.Length);
            }
        }
    }
}