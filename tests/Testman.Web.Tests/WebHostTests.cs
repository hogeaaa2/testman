using Microsoft.AspNetCore.Mvc.Testing;

namespace Testman.Web.Tests;

public sealed class WebHostTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public WebHostTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Home_page_is_available()
    {
        var response = await client.GetAsync("/");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Testman", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Home_page_exposes_the_approved_view_modes_and_local_styles()
    {
        var html = await client.GetStringAsync("/");

        Assert.Contains("Test patterns", html, StringComparison.Ordinal);
        Assert.Contains("Verification", html, StringComparison.Ordinal);
        Assert.Contains("Specification diagnostics", html, StringComparison.Ordinal);
        Assert.Contains("/lib/bootstrap/dist/css/bootstrap.min.", html, StringComparison.Ordinal);
        Assert.DoesNotContain("cdn.", html, StringComparison.OrdinalIgnoreCase);
    }
}
