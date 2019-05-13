// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Try.Protocol.ClientApi
{
    [JsonConverter(typeof(ProjectJsonConverter))]
    public class Project : FeatureContainer
    {
        public string ProjectTemplate { get; }

        public SourceFile[] Files { get; }


        public Project(string projectTemplate, IEnumerable<SourceFile> files)
        {
            if (string.IsNullOrWhiteSpace(projectTemplate))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(projectTemplate));
            }

            if (files == null)
            {
                throw new ArgumentNullException(nameof(files));
            }

            Files = files.Where(f => f != null).ToArray();

            if (Files.Length == 0)
            {
                throw new ArgumentException("Collection cannot be empty.", nameof(files));
            }


            ProjectTemplate = projectTemplate;

        }

        private class ProjectJsonConverter : FeatureContainerConverter<Project>
        {
            protected override void AddProperties(Project container, JObject o)
            {
                o.Add(new JProperty("projectTemplate", container.ProjectTemplate));
                o.Add(new JProperty("files", JArray.FromObject(container.Files)));
            }
        }
    }
}