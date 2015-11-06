using System;
using System.Collections.Generic;
using System.Linq;
using Fclp;
using Fclp.Internals.Extensions;
using LibGit2Sharp;

namespace IsItSpec
{
    internal class Program
    {
        private static readonly IEnumerable<ChangeKind> ValidChangeStatuses = new List<ChangeKind>{ ChangeKind.Added, ChangeKind.Modified, ChangeKind.Renamed }.AsReadOnly();

        private static void Main(string[] args)
        {
            try
            {
                var commitIds = ParseCommitIds(args);

                using (var repo = new Repository("."))
                {
                    var mergeBaseCommit = repo.MergeBaseCommit(commitIds);

                    Console.WriteLine("Using " + mergeBaseCommit + " as merge base.");

                    var patch = repo.Diff.Compare<Patch>(mergeBaseCommit.Tree, repo.Head.Tip.Tree);
                    var valid = patch.Where(ValidChanges).ToList();
                    var specs = valid.Where(IsSpec);
                    var testable = valid.Where(IsTestable);

                    Console.WriteLine();
                    Console.WriteLine("Total changes");
                    patch.ForEach(patchEntryChanges => Console.WriteLine(patchEntryChanges.Status + " -> " + patchEntryChanges.Path));

                    Console.WriteLine();
                    Console.WriteLine("Total valid changes");
                    valid.ForEach(patchEntryChanges => Console.WriteLine(patchEntryChanges.Status + " -> " + patchEntryChanges.Path));

                    Console.WriteLine();
                    Console.WriteLine("testable class changes");
                    testable.ForEach(patchEntryChanges => Console.WriteLine(patchEntryChanges.Status + " -> " + patchEntryChanges.Path));

                    Console.WriteLine();
                    Console.WriteLine("spec changes");
                    specs.ForEach(patchEntryChanges => Console.WriteLine(patchEntryChanges.Status + " -> " + patchEntryChanges.Path));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static bool ValidChanges(PatchEntryChanges patchEntryChanges)
        {
            return ValidChangeStatuses.Contains(patchEntryChanges.Status);
        }

        private static bool IsTestable(PatchEntryChanges patchEntryChanges)
        {
            return patchEntryChanges.Path.EndsWith(".cs") && !IsSpec(patchEntryChanges);
        }

        private static bool IsSpec(PatchEntryChanges patchEntryChanges)
        {
            return patchEntryChanges.Path.EndsWith("Spec.cs");
        }

        private static List<string> ParseCommitIds(string[] args)
        {
            List<string> commitIds = null;
            var argumentParser = new FluentCommandLineParser();

            argumentParser.Setup<List<string>>('c', "commits")
                .Callback(items => commitIds = items);

            argumentParser.Parse(args);

            if (commitIds == null || !commitIds.Any())
            {
                throw new ArgumentException("At least 2 commits needed, none were provided");
            }

            if (commitIds.Count < 1)
            {
                throw new ArgumentException("At least 2 commits needed, only 1 was provided " + commitIds.Single());
            }
            return commitIds;
        }

    }
}
