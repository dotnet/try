// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Try.Protocol;
using Workspace = Microsoft.DotNet.Try.Protocol.Workspace;

namespace Microsoft.DotNet.Try.Project
{

    public static class SourceTextExtensions
    {
        public static IEnumerable<(string id, string content)> ExtractRegions(this SourceText code, string fileName)
        {
            List<(SyntaxTrivia startRegion, SyntaxTrivia endRegion, string label)> FindRegions(SyntaxNode syntaxNode)
            {
                var nodesWithRegionDirectives =
                    from node in syntaxNode.DescendantNodesAndTokens()
                    where node.HasLeadingTrivia
                    from leadingTrivia in node.GetLeadingTrivia()
                    where leadingTrivia.Kind() == SyntaxKind.RegionDirectiveTrivia ||
                          leadingTrivia.Kind() == SyntaxKind.EndRegionDirectiveTrivia
                    select node;

                var regions = new List<(SyntaxTrivia startRegion, SyntaxTrivia endRegion, string label)>();
                var stack = new Stack<SyntaxTrivia>();
                var processedSpans = new HashSet<TextSpan>();
                foreach (var nodeWithRegionDirective in nodesWithRegionDirectives)
                {
                    var triviaList = nodeWithRegionDirective.GetLeadingTrivia();

                    foreach (var currentTrivia in triviaList)
                    {
                        if (!processedSpans.Add(currentTrivia.FullSpan)) continue;

                        if (currentTrivia.Kind() == SyntaxKind.RegionDirectiveTrivia)
                        {
                            stack.Push(currentTrivia);
                        }
                        else if (currentTrivia.Kind() == SyntaxKind.EndRegionDirectiveTrivia)
                        {
                            var start = stack.Pop();
                            var regionName = start.ToFullString().Replace("#region", string.Empty).Trim();
                            var regionId = $"{fileName}@{regionName}";
                            regions.Add(
                                (start, currentTrivia, regionId));
                        }
                    }
                }

                return regions;
            }

            var sourceCodeText = code.ToString();
            var root = CSharpSyntaxTree.ParseText(sourceCodeText).GetRoot();
            var extractedRegions = new List<(string regionId, string content)>();
            foreach (var (startRegion, endRegion, label) in FindRegions(root))
            {
                var start = startRegion.GetLocation().SourceSpan.End;
                var length = endRegion.GetLocation().SourceSpan.Start -
                             startRegion.GetLocation().SourceSpan.End;
                var loc = new TextSpan(start, length);

                var content = code.ToString(loc);

                content = FormatSourceCode(content);
                extractedRegions.Add((label, content));
            }

            return extractedRegions;
        }

        public static IEnumerable<Buffer> ExtractBuffers(this SourceText code, string fileName)
        {
            List<(SyntaxTrivia startRegion, SyntaxTrivia endRegion, string label)> FindRegions(SyntaxNode syntaxNode)
            {
                var nodesWithRegionDirectives =
                    from node in syntaxNode.DescendantNodesAndTokens()
                    where node.HasLeadingTrivia
                    from leadingTrivia in node.GetLeadingTrivia()
                    where leadingTrivia.Kind() == SyntaxKind.RegionDirectiveTrivia ||
                          leadingTrivia.Kind() == SyntaxKind.EndRegionDirectiveTrivia
                    select node;

                var regions = new List<(SyntaxTrivia startRegion, SyntaxTrivia endRegion, string label)>();
                var stack = new Stack<SyntaxTrivia>();
                var processedSpans = new HashSet<TextSpan>();
                foreach (var nodeWithRegionDirective in nodesWithRegionDirectives)
                {
                    var triviaList = nodeWithRegionDirective.GetLeadingTrivia();

                    foreach (var currentTrivia in triviaList)
                    {
                        if (!processedSpans.Add(currentTrivia.FullSpan)) continue;

                        if (currentTrivia.Kind() == SyntaxKind.RegionDirectiveTrivia)
                        {
                            stack.Push(currentTrivia);
                        }
                        else if (currentTrivia.Kind() == SyntaxKind.EndRegionDirectiveTrivia)
                        {
                            var start = stack.Pop();
                            var regionName = start.ToFullString().Replace("#region", string.Empty).Trim();
                            var regionId = $"{fileName}@{regionName}";
                            regions.Add(
                                (start, currentTrivia, regionId));
                        }
                    }
                }

                return regions;
            }

            var sourceCodeText = code.ToString();
            var root = CSharpSyntaxTree.ParseText(sourceCodeText).GetRoot();
            var extractedRegions = new List<Buffer>();
            foreach (var (startRegion, endRegion, label) in FindRegions(root))
            {
                var start = startRegion.GetLocation().SourceSpan.End;
                var length = endRegion.GetLocation().SourceSpan.Start -
                             startRegion.GetLocation().SourceSpan.End;
                var loc = new TextSpan(start, length);

                var content = code.ToString(loc);

                content = FormatSourceCode(content);
                extractedRegions.Add(new Buffer(label, content));
            }

            return extractedRegions;
        }

        private static string FormatSourceCode(string sourceCode)
        {
            var tree = CSharpSyntaxTree.ParseText(sourceCode.Trim(), new CSharpParseOptions(kind: SourceCodeKind.Script));
            var cw = new AdhocWorkspace();
            var formattedCode = Formatter.Format(tree.GetRoot(), cw);
            return formattedCode.ToFullString();
        }
    }
}

