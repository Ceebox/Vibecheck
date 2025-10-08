namespace Vibekiller.Git.Tests;

[TestClass]
public sealed class PatchDifferTests
{
    [TestMethod]
    public void ParsePatchToHunks_SingleHunk_WorksCorrectly()
    {
        var diff = """
                --- a/Example.cs
                +++ b/Example.cs
                @@ -10,5 +10,6 @@
                 public void Foo() {
                 -    Console.WriteLine("old");
                 +    Console.WriteLine("new");
                 +    Console.WriteLine("added");
                  }
                """;

        var hunks = PatchDiffer.ParsePatchToHunks("Example.cs", diff).ToList();

        Assert.IsNotNull(hunks);

        var hunk = hunks[0];
        Assert.AreEqual("Example.cs", hunk.Path);
        Assert.AreEqual(10, hunk.OldStart);
        Assert.AreEqual(5, hunk.OldCount);
        Assert.AreEqual(10, hunk.NewStart);
        Assert.AreEqual(6, hunk.NewCount);

        var added = hunk.Lines.Count(l => l.Type == ChangeType.ADDED);
        var deleted = hunk.Lines.Count(l => l.Type == ChangeType.DELETED);

        Assert.AreEqual(2, added);
        Assert.AreEqual(1, deleted);
    }

    [TestMethod]
    public void ParsePatchToHunks_EmptyPatch_ReturnsEmpty()
    {
        var hunks = PatchDiffer.ParsePatchToHunks("Empty.cs", string.Empty).ToList();
        Assert.AreEqual(0, hunks.Count);
    }

    [TestMethod]
    public void ParsePatchToHunks_HandlesUnmodifiedLines()
    {
        var diff = """
                --- a/Sample.cs
                +++ b/Sample.cs
                @@ -5,3 +5,3 @@
                  unchanged line
                 -old
                 +new
                """;

        var hunk = PatchDiffer.ParsePatchToHunks("Sample.cs", diff).Single();
        Assert.AreEqual(3, hunk.Lines.Count);
        Assert.IsTrue(hunk.Lines.Any(l => l.Type == ChangeType.UNMODIFIED && l.Content == "unchanged line"));
        Assert.IsTrue(hunk.Lines.Any(l => l.Type == ChangeType.DELETED && l.Content == "old"));
        Assert.IsTrue(hunk.Lines.Any(l => l.Type == ChangeType.ADDED && l.Content == "new"));
    }
}
