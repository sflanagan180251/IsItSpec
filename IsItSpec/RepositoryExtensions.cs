using System;
using System.Collections.Generic;
using LibGit2Sharp;

namespace IsItSpec
{
    internal static class RepositoryExtensions
    {
        public static Commit ResolveMergeBaseCommit(this IRepository repo, List<string> commitIds)
        {
            var commits = repo.LookupCommits(commitIds);

            if (commits.Count < 1)
            {
                throw new ArgumentException("At least valid commit needed to find the merge base, none were found in " + string.Join(",", commitIds));
            }

            commits.Add(repo.Head.Tip);

            return repo.Commits.FindMergeBase(commits, MergeBaseFindingStrategy.Standard);
        }

        public static List<Commit> LookupCommits(this IRepository repo, List<string> commitIds)
        {
            var commits = new List<Commit>();

            commitIds.ForEach(commitId =>
            {
                var commit = repo.Lookup<Commit>(commitId);
                if (commit != null)
                {
                    commits.Add(commit);
                }
            });

            return commits;
        }

    }
}