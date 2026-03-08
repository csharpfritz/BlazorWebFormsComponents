using Microsoft.Playwright;

namespace ContosoUniversity.AcceptanceTests;

/// <summary>
/// Verifies that every top-level navigation link in the ContosoUniversity
/// master page resolves to a page that loads without errors.
/// </summary>
[Collection("Playwright")]
public class NavigationTests
{
    private readonly PlaywrightFixture _fixture;

    public NavigationTests(PlaywrightFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task MasterPage_RendersNavLinks()
    {
        var page = await _fixture.NewPageAsync();
        await page.GotoAsync($"{TestConfiguration.BaseUrl}/Home.aspx");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // The master page (#navBar) should contain links to all 5 pages
        var navLinks = page.Locator("#navBar a, nav a");
        var count = await navLinks.CountAsync();

        Assert.True(count >= 5,
            $"Expected at least 5 navigation links in the master page navbar, found {count}");
    }

    [Theory]
    [InlineData("Home.aspx", "home")]
    [InlineData("About.aspx", "about")]
    [InlineData("Students.aspx", "students")]
    [InlineData("Courses.aspx", "courses")]
    [InlineData("Instructors.aspx", "instructors")]
    public async Task NavLink_NavigatesToCorrectPage(string expectedPage, string linkId)
    {
        var page = await _fixture.NewPageAsync();
        await page.GotoAsync($"{TestConfiguration.BaseUrl}/Home.aspx");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Navigation links use IDs: #home, #about, #students, #courses, #instructors
        var link = page.Locator($"#{linkId}");
        if (await link.CountAsync() == 0)
        {
            // Fallback: find by href containing the page name
            link = page.Locator($"a[href*='{expectedPage}']").First;
        }

        Assert.True(await link.CountAsync() > 0,
            $"Navigation link for '{expectedPage}' not found (tried #{linkId} and href match)");

        await link.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        Assert.Contains(expectedPage, page.Url, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("/Home.aspx")]
    [InlineData("/About.aspx")]
    [InlineData("/Students.aspx")]
    [InlineData("/Courses.aspx")]
    [InlineData("/Instructors.aspx")]
    public async Task AllPages_ReturnHttp200(string path)
    {
        var page = await _fixture.NewPageAsync();
        var response = await page.GotoAsync($"{TestConfiguration.BaseUrl}{path}");

        Assert.NotNull(response);
        Assert.True(response.Ok,
            $"{path} returned HTTP {response.Status}, expected 200");
    }
}
