using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Fclp;
using Fclp.Internals.Extensions;
using LibGit2Sharp;

namespace IsItSpec
{
    internal class Program
    {
        private static readonly IEnumerable<ChangeKind> ValidChangeStatuses = new List<ChangeKind>{ ChangeKind.Added, ChangeKind.Modified, ChangeKind.Renamed }.AsReadOnly();
        private static readonly IEnumerable<string> SkipFileNames = new List<string> {"Resources.Designer.cs"};

        private static void Main(string[] args)
        {
            try
            {
                var commitIds = ParseCommitIds(args);

                using (var repo = new Repository("."))
                {
                    var mergeBaseCommit = repo.ResolveMergeBaseCommit(commitIds);

                    Console.WriteLine("Using " + mergeBaseCommit + " as merge base.");

                    var patch = repo.Diff.Compare<Patch>(mergeBaseCommit.Tree, repo.Head.Tip.Tree);
                    var valid = patch.Where(HasValidChangeStatus).ToList();
                    var specs = valid.Where(IsSpec).ToList();
                    var testable = valid.Where(IsTestable).ToList();

                    var testableClassesMissingSpecFiles = testable.Where(testablePatchEntryChange => GetPossibleSpecs(testablePatchEntryChange, specs).Count < 1).ToList();

                    var testableClassesPossiblyMissingSpecs = testable.Where(testablePatchEntryChange => !testableClassesMissingSpecFiles.Contains(testablePatchEntryChange) && !IsTested(testablePatchEntryChange, GetPossibleSpecs(testablePatchEntryChange, specs)));

                    Console.WriteLine();
                    Console.WriteLine("All changes");
                    patch.ForEach(patchEntryChanges => Console.WriteLine(patchEntryChanges.Status + " -> " + patchEntryChanges.Path));

                    Console.WriteLine();
                    Console.WriteLine("Changes with valid change status of " + string.Join(", ", ValidChangeStatuses.ToList().ConvertAll(validChangeStatus => validChangeStatus.ToString())));
                    valid.ForEach(patchEntryChanges => Console.WriteLine(patchEntryChanges.Status + " -> " + patchEntryChanges.Path));

                    Console.WriteLine();
                    Console.WriteLine("testable class changes");
                    testable.ForEach(patchEntryChanges => Console.WriteLine(patchEntryChanges.Status + " -> " + patchEntryChanges.Path));

                    Console.WriteLine();
                    Console.WriteLine("specs found");
                    specs.ForEach(patchEntryChanges => Console.WriteLine(patchEntryChanges.Status + " -> " + patchEntryChanges.Path));

                    Console.WriteLine();
                    Console.WriteLine("testable classes with missing specs");
                    testableClassesMissingSpecFiles.ForEach(patchEntryChanges => Console.WriteLine(patchEntryChanges.Status + " -> " + patchEntryChanges.Path));

                    Console.WriteLine();
                    Console.WriteLine("testable classes with possibly missing specs");
                    testableClassesPossiblyMissingSpecs.ForEach(patchEntryChanges => Console.WriteLine(patchEntryChanges.Status + " -> " + patchEntryChanges.Path));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static bool IsTested(PatchEntryChanges testablePatchEntryChange, List<PatchEntryChanges> possibleSpecs)
        {
            const string usingName = "usingName";
            const string speccedTypeName = "speccedType";

            const string usingRegex = @"using\s+(?<" + usingName + @">\S+)\;";
            const string speccedTypeRegex = @"\s+Spec\<(?<" + speccedTypeName + @">\S+)\>";

            var testableName = ExtractFullClassName(testablePatchEntryChange);

            foreach (var possibleSpec in possibleSpecs)
            {
                var content = File.ReadAllText(possibleSpec.Path);

                var usingNamespaces = Regex.Matches(content, usingRegex, RegexOptions.Singleline).Cast<Match>().Select(match => match.Groups).Select(group => group[usingName]).ToList();

                var speccedType = Regex.Match(content, speccedTypeRegex, RegexOptions.Singleline).Groups[speccedTypeName];

                if (usingNamespaces.Select(usingNamespace => usingNamespace + "." + speccedType).Any(fullySpeccedName => testableName == fullySpeccedName))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<PatchEntryChanges> GetPossibleSpecs(PatchEntryChanges testablePatchEntryChange, List<PatchEntryChanges> specs)
        {
            var expectedSpecFilename = Path.GetFileNameWithoutExtension(testablePatchEntryChange.Path) + "Spec.cs";

            var possibleSpecs = specs.Where(specPatchEntryChange => specPatchEntryChange.Path.EndsWith(expectedSpecFilename)).ToList();
            return possibleSpecs;
        }

        private static string ExtractFullClassName(PatchEntryChanges patchEntryChanges)
        {
            const string namespaceName = "namespaceName";
            const string className = "className";

            const string fullNameRegex =   @"namespace\s+(?<"+ namespaceName + @">\S*).*\sclass\s+(?<" + className + @">\S+)";

            var content = File.ReadAllText(patchEntryChanges.Path);

            var match = Regex.Match(content, fullNameRegex, RegexOptions.Singleline);

            return match.Groups[namespaceName] + "." + match.Groups[className];
        }

        private static bool HasValidChangeStatus(PatchEntryChanges patchEntryChanges)
        {
            return ValidChangeStatuses.Contains(patchEntryChanges.Status);
        }

        private static bool IsSkipped(PatchEntryChanges patchEntryChanges)
        {
            return SkipFileNames.Any(skipFileName => patchEntryChanges.Path.EndsWith(skipFileName));
        }

        private static bool IsTestable(PatchEntryChanges patchEntryChanges)
        {
            return patchEntryChanges.Path.EndsWith(".cs") && !IsSkipped(patchEntryChanges) && !IsSpec(patchEntryChanges) && !IsInterface(patchEntryChanges);
        }

        private static bool IsInterface(PatchEntryChanges patchEntryChanges)
        {
            const string interfaceRegex = @"\s+(internal|public)\s+interface\s+I.*";

            var content = File.ReadAllText(patchEntryChanges.Path);

            var result = Regex.IsMatch(content, interfaceRegex);

            return result;
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
                throw new ArgumentException("At least 1 commit needed, none were provided");
            }

            return commitIds;
        }

    }
}
