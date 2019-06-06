using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Buffer = Microsoft.DotNet.Try.Protocol.Buffer;

namespace Microsoft.DotNet.Try.Project
{
    public class FSharpBufferInliningTransformer : BufferInliningTransformer
    {
        protected override Task InjectBufferAtSpan(Viewport viewPort, Buffer sourceBuffer, ICollection<Buffer> buffers, IDictionary<string, SourceFile> files, TextSpan span)
        {
            var replacementPosition = viewPort.Destination.Text.Lines.GetLinePosition(viewPort.OuterRegion.Start);
            var indentLevel = replacementPosition.Character;
            var indentText = new string(' ', indentLevel);
            var indentedLines = sourceBuffer.Content.Split('\n').Select(l => indentText + l).ToList();
            var indentedText = string.Join("\n", indentedLines);
            var textChange = new TextChange(span, indentedText);
            var newText = viewPort.Destination.Text.WithChanges(textChange);
            buffers.Add(new Buffer(
                sourceBuffer.Id,
                sourceBuffer.Content,
                sourceBuffer.Position,
                span.Start));
            files[viewPort.Destination.Name] = SourceFile.Create(newText.ToString(), viewPort.Destination.Name);
            return Task.CompletedTask;
        }
    }
}
