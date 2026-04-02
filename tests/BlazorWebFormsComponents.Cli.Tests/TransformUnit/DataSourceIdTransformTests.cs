using BlazorWebFormsComponents.Cli.Pipeline;
using BlazorWebFormsComponents.Cli.Transforms.Markup;

namespace BlazorWebFormsComponents.Cli.Tests.TransformUnit;

/// <summary>
/// Unit tests for DataSourceIdTransform — replaces DataSourceID with Items binding
/// and scaffolds compilable data properties.
/// </summary>
public class DataSourceIdTransformTests
{
    private readonly DataSourceIdTransform _transform = new();

    private static FileMetadata MakeMetadata(string? codeBehind = null) => new()
    {
        SourceFilePath = "test.aspx",
        OutputFilePath = "test.razor",
        FileType = FileType.Page,
        OriginalContent = "",
        CodeBehindContent = codeBehind
    };

    [Fact]
    public void HasCorrectOrder()
    {
        Assert.Equal(820, _transform.Order);
    }

    [Fact]
    public void HasCorrectName()
    {
        Assert.Equal("DataSourceId", _transform.Name);
    }

    [Fact]
    public void ReplacesDataSourceIdWithItemsBinding()
    {
        var input = @"<GridView id=""gvProducts"" DataSourceID=""SqlDS1"">";
        var result = _transform.Apply(input, MakeMetadata());

        Assert.Contains(@"Items=""@SqlDS1Data""", result);
        Assert.DoesNotContain("DataSourceID", result);
    }

    [Fact]
    public void RemovesSelfClosingDataSourceControl()
    {
        var input = @"<SqlDataSource id=""SqlDS1"" ConnectionString=""test"" />";
        var result = _transform.Apply(input, MakeMetadata());

        Assert.DoesNotContain("<SqlDataSource", result);
        Assert.Contains("TODO(bwfc-datasource)", result);
    }

    [Fact]
    public void RemovesOpenCloseDataSourceControl()
    {
        var input = "<ObjectDataSource id=\"ODS1\">\n    <SelectParameters>\n    </SelectParameters>\n</ObjectDataSource>";
        var result = _transform.Apply(input, MakeMetadata());

        Assert.DoesNotContain("<ObjectDataSource", result);
        Assert.Contains("TODO(bwfc-datasource)", result);
        Assert.Contains("Implement IODS1DataService", result);
    }

    [Fact]
    public void DataSourceTodoIncludesSpecificServiceName()
    {
        var input = @"<SqlDataSource id=""ProductsSource"" SelectCommand=""SELECT * FROM Products"" />";
        var result = _transform.Apply(input, MakeMetadata());

        Assert.Contains("Implement IProductsSourceDataService to replace SqlDataSource", result);
    }

    [Fact]
    public void DataSourceTodoFallsBackWhenNoId()
    {
        var input = @"<SqlDataSource SelectCommand=""SELECT 1"" />";
        var result = _transform.Apply(input, MakeMetadata());

        Assert.Contains("<SqlDataSource> has no Blazor equivalent", result);
    }

    [Fact]
    public void InjectsCodeBlockWhenNoCodeBehind()
    {
        var input = @"<GridView DataSourceID=""SqlDS1"" />";
        var result = _transform.Apply(input, MakeMetadata());

        Assert.Contains("@code {", result);
        Assert.Contains("private IEnumerable<object> SqlDS1Data { get; set; } = Array.Empty<object>();", result);
        Assert.Contains("// TODO(bwfc-datasource): Replace SqlDS1Data with real data from injected service", result);
    }

    [Fact]
    public void InjectsIntoCodeBehindWhenPresent()
    {
        var codeBehind = "public partial class TestPage\n{\n    protected void Page_Load(object sender, EventArgs e) { }\n}";
        var metadata = MakeMetadata(codeBehind);
        var input = @"<GridView DataSourceID=""SqlDS1"" />";

        _transform.Apply(input, metadata);

        Assert.NotNull(metadata.CodeBehindContent);
        Assert.Contains("private IEnumerable<object> SqlDS1Data { get; set; } = Array.Empty<object>();", metadata.CodeBehindContent);
        Assert.Contains("// TODO(bwfc-datasource): Replace SqlDS1Data with real data from injected service", metadata.CodeBehindContent);
    }

    [Fact]
    public void DoesNotInjectCodeBlockWhenNoDataSourceIds()
    {
        var input = @"<GridView id=""gvProducts"" />";
        var result = _transform.Apply(input, MakeMetadata());

        Assert.DoesNotContain("@code", result);
    }

    [Fact]
    public void HandlesMultipleDataSourceIds()
    {
        var input = @"<GridView DataSourceID=""DS1"" /><DropDownList DataSourceID=""DS2"" />";
        var result = _transform.Apply(input, MakeMetadata());

        Assert.Contains(@"Items=""@DS1Data""", result);
        Assert.Contains(@"Items=""@DS2Data""", result);
        Assert.Contains("private IEnumerable<object> DS1Data", result);
        Assert.Contains("private IEnumerable<object> DS2Data", result);
    }

    [Fact]
    public void HandlesDuplicateDataSourceIdReferences()
    {
        var input = "<GridView DataSourceID=\"SharedDS\" />\n<DropDownList DataSourceID=\"SharedDS\" />";
        var result = _transform.Apply(input, MakeMetadata());

        // Should only inject one property for the shared ID
        var occurrences = result.Split("private IEnumerable<object> SharedDSData").Length - 1;
        Assert.Equal(1, occurrences);
    }

    [Fact]
    public void HandlesAllDataSourceControlTypes()
    {
        var controls = new[] { "SqlDataSource", "ObjectDataSource", "LinqDataSource",
            "EntityDataSource", "XmlDataSource", "SiteMapDataSource", "AccessDataSource" };

        foreach (var ctrl in controls)
        {
            var input = $@"<{ctrl} id=""Test1"" />";
            var result = _transform.Apply(input, MakeMetadata());

            Assert.DoesNotContain($"<{ctrl}", result);
            Assert.Contains("TODO(bwfc-datasource)", result);
        }
    }

    [Fact]
    public void DoesNotModifyContentWithoutDataSourceAttributes()
    {
        var input = @"<GridView id=""gvProducts"" CssClass=""table"" />";
        var result = _transform.Apply(input, MakeMetadata());

        Assert.Equal(input, result);
    }

    [Fact]
    public void CodeBehindInjectionPreservesExistingContent()
    {
        var codeBehind = "public partial class TestPage\n{\n    protected void Page_Load(object sender, EventArgs e) { }\n}";
        var metadata = MakeMetadata(codeBehind);
        var input = @"<GridView DataSourceID=""SqlDS1"" />";

        _transform.Apply(input, metadata);

        Assert.NotNull(metadata.CodeBehindContent);
        Assert.Contains("Page_Load", metadata.CodeBehindContent);
    }
}
