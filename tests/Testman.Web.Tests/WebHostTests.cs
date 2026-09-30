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
}
