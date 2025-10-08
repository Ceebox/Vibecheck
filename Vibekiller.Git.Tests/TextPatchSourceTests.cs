namespace Vibekiller.Git.Tests;

[TestClass]
public sealed class TextPatchSourceTests
{
    [TestMethod]
    public void GetPatchInfo_SingleFilePatch_ParsesCorrectly()
    {
        var patch = """
                --- a/Example.cs
                +++ b/Example.cs
                @@ -10,5 +10,6 @@
                 public void Foo() {
                 -    Console.WriteLine("old");
                 +    Console.WriteLine("new");
                 +    Console.WriteLine("added");
                  }
                """;

        var source = new TextPatchSource(patch);
        var patches = source.GetPatchInfo().ToList();

        Assert.AreEqual(1, patches.Count);
        Assert.AreEqual("Example.cs", patches[0].Path);
        StringAssert.Contains(patches[0].Contents, "@@ -10,5 +10,6 @@");
    }

    [TestMethod]
    public void GetPatchInfo_MultipleFiles_ParsesEachFile()
    {
        var patch = """
                --- a/One.cs
                +++ b/One.cs
                @@ -1,1 +1,1 @@
                 -old
                 +new

                --- a/Two.cs
                +++ b/Two.cs
                @@ -2,2 +2,3 @@
                 -remove
                 +add
                 +another
                """;

        var source = new TextPatchSource(patch);
        var patches = source.GetPatchInfo().ToList();

        Assert.AreEqual(2, patches.Count);
        Assert.AreEqual("One.cs", patches[0].Path);
        Assert.AreEqual("Two.cs", patches[1].Path);

        StringAssert.Contains(patches[0].Contents, "-old");
        StringAssert.Contains(patches[1].Contents, "+another");
    }

    [TestMethod]
    public void GetPatchInfo_EmptyInput_ReturnsEmpty()
    {
        var source = new TextPatchSource(string.Empty);
        var patches = source.GetPatchInfo().ToList();

        Assert.AreEqual(0, patches.Count);
    }

    [TestMethod]
    public void GetPatchInfo_HandlesWindowsLineEndings()
    {
        var patch = "--- a/File.cs\r\n+++ b/File.cs\r\n@@ -1,1 +1,1 @@\r\n-old\r\n+new\r\n";
        var source = new TextPatchSource(patch);
        var patches = source.GetPatchInfo().ToList();

        Assert.AreEqual(1, patches.Count);
        Assert.AreEqual("File.cs", patches[0].Path);
    }
}