// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace MLS.Repositories
{
    public interface IRepoLocator
    {
        Task<IEnumerable<Repo>> LocateRepo(string repo);
    }

    public class Repo
    {
        public Repo(string name, string cloneUrl)
        {
            Name = name;
            CloneUrl = cloneUrl;
        }

        public string Name { get; }
        public string CloneUrl { get; }
    }
}
