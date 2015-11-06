using System;
using System.Collections.Generic;
using LibGit2Sharp;

namespace IsItSpec
{
    internal static class RepositoryExtensions
    {
        public static Commit MergeBaseCommit(this IRepository repo, List<string> commitIds)
        {
            var commits = repo.LookupCommits(commitIds);

            if (commits.Count < 2)
            {
                throw new ArgumentException("At least 2 valid commits needed to find the merge base, not enough were found in " + string.Join(",", commitIds));
            }

            var mergeBaseCommit = repo.Commits.FindMergeBase(commits, MergeBaseFindingStrategy.Standard);
            return mergeBaseCommit;
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