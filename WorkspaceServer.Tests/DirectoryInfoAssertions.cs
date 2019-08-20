// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using System.IO;
using Microsoft.DotNet.Try.Markdown;

namespace WorkspaceServer.Tests
{
    public class DirectoryInfoAssertions :
ReferenceTypeAssertions<DirectoryInfo, DirectoryInfoAssertions>
    {
        public DirectoryInfoAssertions(DirectoryInfo instance)
        {
            Subject = instance;
        }

        protected override string Identifier => "directory";

        public AndConstraint<DirectoryInfoAssertions> BeNormalizedEqualTo(
            DirectoryInfo dirInfo, string because = "", params object[] becauseArgs)
        {
            var thisDirPath = RelativePath.NormalizeDirectory(Subject.FullName);
            var compareDirPath = RelativePath.NormalizeDirectory(dirInfo.FullName);

            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(dirInfo != null)
                .FailWith("Pass a non-null directory info")
                .Then
                .Given(() => thisDirPath)
                .ForCondition(fullpath => fullpath == compareDirPath)
                .FailWith($"Expected {thisDirPath} to be equivalent to {compareDirPath}");

            return new AndConstraint<DirectoryInfoAssertions>(this);
        }

        public AndConstraint<DirectoryInfoAssertions> BeNormalizedEqualTo(
           string directoryPath, string because = "", params object[] becauseArgs)
        {
            return BeNormalizedEqualTo(new DirectoryInfo(directoryPath), because, becauseArgs);
        }
    }
}