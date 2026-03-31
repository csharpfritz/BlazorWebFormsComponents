namespace BlazorWebFormsComponents.Cli.Tests;

/// <summary>
/// Parameterized L1 transform acceptance tests.
/// Each TC* test case reads input .aspx (and optional .aspx.cs), runs the full
/// transform pipeline, and compares against expected .razor (and optional .razor.cs).
///
/// These are the gate tests — the C# tool MUST pass all cases before the
/// PowerShell migration script is deprecated.
/// </summary>
public class L1TransformTests
{
    private static readonly string TestDataRoot = TestHelpers.GetTestDataRoot();

    /// <summary>
    /// Provides all markup test case names for [Theory] parameterization.
    /// Discovers TC* files from TestData/inputs/*.aspx.
    /// </summary>
    public static IEnumerable<object[]> GetMarkupTestCases()
    {
        return TestHelpers.DiscoverTestCases()
            .Select(name => new object[] { name });
    }

    /// <summary>
    /// Provides code-behind test case names (only those with .aspx.cs + .razor.cs pairs).
    /// </summary>
    public static IEnumerable<object[]> GetCodeBehindTestCases()
    {
        return TestHelpers.DiscoverCodeBehindTestCases()
            .Select(name => new object[] { name });
    }

    // TODO: Uncomment when MigrationPipeline is built by Bishop.
    // private readonly MigrationPipeline _pipeline = TestHelpers.CreateDefaultPipeline();

    [Theory]
    [MemberData(nameof(GetMarkupTestCases))]
    public void L1Transform_ProducesExpectedMarkup(string testCaseName)
    {
        // Arrange
        var inputPath = Path.Combine(TestDataRoot, "inputs", $"{testCaseName}.aspx");
        var expectedPath = Path.Combine(TestDataRoot, "expected", $"{testCaseName}.razor");

        Assert.True(File.Exists(inputPath), $"Input file not found: {inputPath}");
        Assert.True(File.Exists(expectedPath), $"Expected file not found: {expectedPath}");

        var input = File.ReadAllText(inputPath);
        var expected = TestHelpers.NormalizeContent(File.ReadAllText(expectedPath));

        // Act
        // TODO: Replace with pipeline call when MigrationPipeline is built:
        //   var metadata = new FileMetadata { SourceFilePath = inputPath, FileType = FileType.Page };
        //   var result = _pipeline.TransformMarkup(input, metadata);
        //   var actual = TestHelpers.NormalizeContent(result);
        //
        // For now, verify test infrastructure works by asserting files are readable
        Assert.False(string.IsNullOrWhiteSpace(input), "Input file should not be empty");
        Assert.False(string.IsNullOrWhiteSpace(expected), "Expected file should not be empty");

        // Placeholder assertion — will be replaced with actual transform comparison.
        // This ensures the test data is properly discovered and loadable.
        Assert.NotEqual(expected, TestHelpers.NormalizeContent(input));
    }

    [Theory]
    [MemberData(nameof(GetCodeBehindTestCases))]
    public void L1Transform_ProducesExpectedCodeBehind(string testCaseName)
    {
        // Arrange
        var inputCsPath = Path.Combine(TestDataRoot, "inputs", $"{testCaseName}.aspx.cs");
        var expectedCsPath = Path.Combine(TestDataRoot, "expected", $"{testCaseName}.razor.cs");

        Assert.True(File.Exists(inputCsPath), $"Input code-behind not found: {inputCsPath}");
        Assert.True(File.Exists(expectedCsPath), $"Expected code-behind not found: {expectedCsPath}");

        var inputCs = File.ReadAllText(inputCsPath);
        var expectedCs = TestHelpers.NormalizeContent(File.ReadAllText(expectedCsPath));

        // Act
        // TODO: Replace with pipeline call when code-behind transforms are built:
        //   var metadata = new FileMetadata { SourceFilePath = inputCsPath, FileType = FileType.CodeBehind };
        //   var result = _pipeline.TransformCodeBehind(inputCs, metadata);
        //   var actualCs = TestHelpers.NormalizeContent(result);
        //   Assert.Equal(expectedCs, actualCs);
        //
        // For now, verify the test data is properly paired and loadable
        Assert.False(string.IsNullOrWhiteSpace(inputCs), "Input code-behind should not be empty");
        Assert.False(string.IsNullOrWhiteSpace(expectedCs), "Expected code-behind should not be empty");

        // Placeholder assertion — input and expected should differ (transforms change content)
        Assert.NotEqual(expectedCs, TestHelpers.NormalizeContent(inputCs));
    }

    [Fact]
    public void TestData_ContainsExpectedNumberOfTestCases()
    {
        // Verify we have the expected 21 markup test cases (TC01–TC21)
        var testCases = TestHelpers.DiscoverTestCases().ToList();
        Assert.Equal(21, testCases.Count);
    }

    [Fact]
    public void TestData_AllInputsHaveExpectedOutputs()
    {
        var expectedDir = Path.Combine(TestDataRoot, "expected");
        foreach (var tc in TestHelpers.DiscoverTestCases())
        {
            var expectedPath = Path.Combine(expectedDir, $"{tc}.razor");
            Assert.True(File.Exists(expectedPath),
                $"Test case '{tc}' has input .aspx but no expected .razor");
        }
    }

    [Fact]
    public void TestData_CodeBehindPairsAreComplete()
    {
        var inputDir = Path.Combine(TestDataRoot, "inputs");
        var expectedDir = Path.Combine(TestDataRoot, "expected");

        var inputCsFiles = Directory.GetFiles(inputDir, "*.aspx.cs")
            .Select(f => Path.GetFileName(f).Replace(".aspx.cs", ""))
            .ToList();

        // Every .aspx.cs input should have a corresponding .razor.cs expected output
        foreach (var tc in inputCsFiles)
        {
            Assert.True(File.Exists(Path.Combine(expectedDir, $"{tc}.razor.cs")),
                $"Test case '{tc}' has input .aspx.cs but no expected .razor.cs");
        }

        // Verify we have 8 code-behind test cases (TC13–TC16, TC18–TC21)
        Assert.Equal(8, inputCsFiles.Count);
    }
}
