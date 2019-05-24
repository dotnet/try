using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Try.Protocol;
using Buffer = Microsoft.DotNet.Try.Protocol.Buffer;

namespace Microsoft.DotNet.Try.Project
{
    internal class FSharpMethods
    {
        private const string FSharpRegionStart = "//#region";
        private const string FSharpRegionEnd = "//#endregion";

        public static IEnumerable<Buffer> ExtractBuffers(SourceText code, string fileName)
        {
            var extractedBuffers = new List<Buffer>();
            foreach ((var bufferId, var contentSpan, var regionSpan) in ExtractRegions(code, fileName))
            {
                var content = code.ToString(contentSpan);
                extractedBuffers.Add(new Buffer(bufferId, content));
            }

            return extractedBuffers;
        }

        public static IEnumerable<(BufferId bufferId, TextSpan span, TextSpan outerSpan)> ExtractRegions(SourceText code, string fileName)
        {
            var extractedRegions = new List<(BufferId, TextSpan, TextSpan)>();
            var text = code.ToString();
            int regionTagStartIndex = text.IndexOf(FSharpRegionStart);
            while (regionTagStartIndex > 0)
            {
                var regionLabelEndIndex = text.IndexOf('\n', regionTagStartIndex);
                var regionLabelStartIndex = regionTagStartIndex + FSharpRegionStart.Length;
                var regionLabel = text.Substring(regionLabelStartIndex, regionLabelEndIndex - regionLabelStartIndex).Trim();
                var regionTagEndIndex = text.IndexOf(FSharpRegionEnd, regionTagStartIndex);
                if (regionTagEndIndex > 0)
                {
                    var regionEndTagLastIndex = regionTagEndIndex + FSharpRegionEnd.Length;
                    var contentSpan = new TextSpan(regionLabelEndIndex, regionTagEndIndex - regionLabelEndIndex);
                    var regionSpan = new TextSpan(regionTagStartIndex, regionEndTagLastIndex - regionTagStartIndex);
                    extractedRegions.Add((new BufferId(fileName, regionLabel), contentSpan, regionSpan));

                    regionTagStartIndex = text.IndexOf(FSharpRegionStart, regionTagEndIndex);
                }
                else
                {
                    break;
                }
            }

            return extractedRegions;
        }

        private const string NewLinePadding = "\n";

        internal static Task InjectBufferAtSpan(Viewport viewPort, Buffer sourceBuffer, ICollection<Buffer> buffers, IDictionary<string, SourceFile> files, TextSpan span)
        {
            var replacementPosition = viewPort.Destination.Text.Lines.GetLinePosition(viewPort.OuterRegion.Start);
            var indentLevel = replacementPosition.Character;
            var indentText = new string(' ', indentLevel);
            var indentedLines = sourceBuffer.Content.Split('\n').Select(l => indentText + l).ToList();
            var indentedText =
                NewLinePadding + // leading `//#region [label]` ends with '\r'; '\n' is necessary
                string.Join("\n", indentedLines) +
                Environment.NewLine + indentText; // ensure that the trailing `//#endregion` retains its indention level
            var textChange = new TextChange(span, indentedText);
            var newText = viewPort.Destination.Text.WithChanges(textChange);
            buffers.Add(new Buffer(
                sourceBuffer.Id,
                sourceBuffer.Content,
                sourceBuffer.Position,
                span.Start + NewLinePadding.Length));
            files[viewPort.Destination.Name] = SourceFile.Create(newText.ToString(), viewPort.Destination.Name);
            return Task.CompletedTask;
        }
    }
}
