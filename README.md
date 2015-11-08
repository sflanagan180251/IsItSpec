# IsItSpec
2015-11 Hackathon competition to determine if code in the current branch has been unit tested via SpecEasy

This is a tool meant to help determine what unit test work is needed before a PR should be considered tested.

For now, it is confused by generics and I've run out of time to fix this for the hackathon, but I suspect I can use
regex to replace the <T> class generic type with <.+> and see if they match for slight improvements in positive
matching.

Usage: IsItSpec -commit <commit> [<commit>]...

This will determine the merge base from the current HEAD and the provided commit(s) to determine the changes
that should be scanned.  From that all changes are evaluated to determine if they are testable classes (i.e. 
.cs class files.) and if so, do we have a SpecEasy class that tests it.  
