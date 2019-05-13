// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace MLS.Repositories
{
    public class RepoLocatorSimulator : IRepoLocator
    {
        public Task<IEnumerable<Repo>> LocateRepo(string repo)
        {
            var result = new List<Repo>();
            if (repo.Contains("2660eaec-6af8-452d-b70d-41227d616cd9"))
            {
                result.Add(
                    new Repo("rchande/2660eaec-6af8-452d-b70d-41227d616cd9",
                "https://github.com/rchande/2660eaec-6af8-452d-b70d-41227d616cd9.git"));
            }
            else if (repo == "rchande/tribble")
            {
                result.Add(
                    new Repo("rchande/upgraded-octo-tribble.",
                "https://github.com/rchande/upgraded-octo-tribble..git"));
                result.Add(
                    new Repo("rchande/downgraded-octo-tribble.",
                "https://github.com/rchande/downgraded-octo-tribble..git"));
            }

            return Task.FromResult((IEnumerable<Repo>)result);
        }
    }
}
