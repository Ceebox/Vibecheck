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

    [<TestMethod>]
    member _.ExtractObjectsAsArray_WithSingleObject_ReturnsSingleItem() =
        let input = """
        {
            "has_change": true,
            "comment": "Test single object"
        }"""
        let result = JsonArrayExtractor.ExtractObjectsAsArray(input)
        Assert.AreEqual<int>(1, result.Length)
        Assert.IsTrue(result[0].Contains("\"has_change\": true"))
        Assert.IsTrue(result[0].Contains("Test single object"))

    [<TestMethod>]
    member _.ExtractObjectsAsArray_WithMultipleObjects_ReturnsAllItems() =
        let input = """
        {
          "has_change": true,
          "comment": "First object"
        },
        {
          "has_change": false,
          "comment": "Second object"
        },
        {
          "has_change": true,
          "comment": "Third object"
        }"""
        let result = JsonArrayExtractor.ExtractObjectsAsArray(input)
        Assert.AreEqual<int>(3, result.Length)
        Assert.IsTrue(result[0].Contains("First object"))
        Assert.IsTrue(result[1].Contains("Second object"))
        Assert.IsTrue(result[2].Contains("Third object"))

    [<TestMethod>]
    member _.ExtractObjectsAsArray_WithInvalidText_SkipsNonObjects() =
        let input = """
        Random text before
        {
            "has_change": true
        }
        Some trailing text
        {
            "has_change": false
        }"""
        let result = JsonArrayExtractor.ExtractObjectsAsArray(input)
        Assert.AreEqual<int>(2, result.Length)
        Assert.IsTrue(result[0].Contains("\"has_change\": true"))
        Assert.IsTrue(result[1].Contains("\"has_change\": false"))
        Assert.IsFalse(result[0].Contains("Random text before"))

    [<TestMethod>]
    member _.ExtractObjectsAsArray_EmptyInput_ReturnsEmptyArray() =
        let result = JsonArrayExtractor.ExtractObjectsAsArray("")
        Assert.AreEqual<int>(0, result.Length)

    [<TestMethod>]
    member _.ExtractObjectsAsArray_MalformedObject_SkipsIncomplete() =
        let input = """
        {
          "has_change": true
        },
        { incomplete
        {
          "has_change": false
        }"""
        let result = JsonArrayExtractor.ExtractObjectsAsArray(input)
        Assert.AreEqual<int>(2, result.Length)
        Assert.IsTrue(result[0].Contains("\"has_change\": true"))
        Assert.IsTrue(result[1].Contains("\"has_change\": false"))
        Assert.IsFalse(result[0].Contains("incomplete"))

    [<TestMethod>]
    member _.ExtractObjectsAsArray_NestedObjects_IncludesNested() =
        let input = """
        {
          "parent": {
            "child": {
              "value": 123
            }
          }
        },
        {
          "another": "object"
        }"""
        let result = JsonArrayExtractor.ExtractObjectsAsArray(input)
        Assert.AreEqual<int>(2, result.Length)
        Assert.IsTrue(result[0].Contains("\"child\": {"))
        Assert.IsTrue(result[1].Contains("\"another\": \"object\""))