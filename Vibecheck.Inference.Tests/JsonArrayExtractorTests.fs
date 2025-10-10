namespace Vibecheck.Inference.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting
open Vibecheck.Utility

[<TestClass>]
type JsonArrayExtractorTests () =

    [<TestMethod>]
    member _.ExtractFirstCompleteArray_WithValidObject_ReturnsArrayWrapped() =
        let input = """
    {
      "has_change": true,
      "suggested_change": null,
      "comment": "Consider using a more specific path for the default value, such as './' or '.' to avoid potential issues with relative paths.",
      "ai_probability": 0.2
    }
]"""
        let result = JsonArrayExtractor.ExtractFirstCompleteArray(input)
        match result with
        | null -> Assert.Fail("result is null")
        | s ->
            Assert.IsTrue(s.StartsWith("["))
            Assert.IsTrue(s.EndsWith("]"))
            Assert.IsTrue(s.Contains("\"has_change\": true"))
            Assert.IsTrue(s.Contains("\"ai_probability\": 0.2"))

    [<TestMethod>]
    member _.ExtractFirstCompleteArray_WithValidObject_SkipInvalidStart() =
        let input = """
    <|eot_id|>
    {
        "has_change": true,
        "suggested_change": null,
        "comment": "Introduce a named parameter for the onlyShowChanges flag, making it more readable and allowing callers to specify this behavior explicitly.",
        "ai_probability": 0.2
    }
]"""
        let result = JsonArrayExtractor.ExtractFirstCompleteArray(input)
        match result with
        | null -> Assert.Fail("result is null")
        | s ->
            Assert.IsTrue(s.StartsWith("["))
            Assert.IsTrue(s.EndsWith("]"))
            Assert.IsTrue(s.Contains("\"has_change\": true"))
            Assert.IsTrue(s.Contains("\"ai_probability\": 0.2"))

    [<TestMethod>]
    member _.ExtractFirstCompleteArray_EmptyInput_ReturnsNull() =
        let result = JsonArrayExtractor.ExtractFirstCompleteArray("")
        Assert.IsNull(result)

    [<TestMethod>]
    member _.ExtractFirstCompleteArray_NestedArray_ReturnsFirstCompleteArray() =
        let input = "Random text [1, 2, [3, 4]] after"
        let result = JsonArrayExtractor.ExtractFirstCompleteArray(input)
        Assert.AreEqual(box "[1, 2, [3, 4]]", box result)

    [<TestMethod>]
    member _.ExtractFirstCompleteArray_MalformedInput_ReturnsNull() =
        let input = "Some text { incomplete json "
        let result = JsonArrayExtractor.ExtractFirstCompleteArray(input)
        Assert.IsNull(result)
