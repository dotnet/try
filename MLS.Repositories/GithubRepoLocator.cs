// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Octokit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MLS.Repositories
{
    public class GitHubRepoLocator : IRepoLocator
    {
        public async Task<IEnumerable<Repo>> LocateRepo(string repo)
        {
            var req = new SearchRepositoriesRequest(repo);
            try
            {
                // TODO: Should we be forcing users to log in (and thereby use their own API token)?
                var client = new GitHubClient(new ProductHeaderValue("github-try-demo"));

                var result = await client.Search.SearchRepo(req);
                return result.Items.Select(i => new Repo(i.FullName, i.CloneUrl));
            }
            catch (RateLimitExceededException)
            {
                return Enumerable.Empty<Repo>();
            }
        }
    }
}
