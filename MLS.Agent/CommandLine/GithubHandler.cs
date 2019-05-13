// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MLS.Repositories;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;

namespace MLS.Agent.CommandLine
{
    public static class GitHubHandler
    {
        public static async Task Handler(TryGitHubOptions options, IConsole console, IRepoLocator locator)
        {
            var repos = (await locator.LocateRepo(options.Repo)).ToArray();

            if (repos.Length == 0)
            {
                console.Out.WriteLine($"Didn't find any repos called `{options.Repo}`");
            }
            else if (repos[0].Name == options.Repo)
            {
                console.Out.WriteLine(GenerateCommandExample(repos[0].Name, repos[0].CloneUrl));

            }
            else
            {
                console.Out.WriteLine("Which of the following did you mean?");
                foreach (var instance in repos)
                {
                    console.Out.WriteLine($"\t{instance.Name}");
                }
            }

            string GenerateCommandExample(string name, string cloneUrl)
            {
                var text = $"Found repo `{name}`\n";
                text += $"To try `{name}`, cd to your desired directory and run the following command:\n\n";
                text += $"\tgit clone {cloneUrl} && dotnet try .";

                return text;
            }
        }
    }
}
