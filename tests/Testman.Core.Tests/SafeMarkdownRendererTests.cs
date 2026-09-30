using Testman.Core.Rendering;

namespace Testman.Core.Tests;

public sealed class SafeMarkdownRendererTests
{
    [Fact]
    public void Render_converts_github_flavored_markdown()
    {
        var html = SafeMarkdownRenderer.Render("**Important**\n\n~~obsolete~~");

        Assert.Contains("<strong>Important</strong>", html, StringComparison.Ordinal);
        Assert.Contains("<del>obsolete</del>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_preserves_approved_html()
    {
        var html = SafeMarkdownRenderer.Render("<details><summary>More</summary><kbd>Enter</kbd></details>");

        Assert.Contains("<details>", html, StringComparison.Ordinal);
        Assert.Contains("<summary>More</summary>", html, StringComparison.Ordinal);
        Assert.Contains("<kbd>Enter</kbd>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_removes_script_and_event_attributes()
    {
        var html = SafeMarkdownRenderer.Render("<script>alert('x')</script><p onclick=\"alert('x')\">Safe</p>");

        Assert.DoesNotContain("script", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alert", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<p>Safe</p>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_removes_javascript_urls()
    {
        var html = SafeMarkdownRenderer.Render("[unsafe](javascript:alert('x'))");

        Assert.DoesNotContain("javascript:", html, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("[relative](./guide.md)", "href=\"./guide.md\"")]
    [InlineData("[secure](https://example.com)", "href=\"https://example.com\"")]
    [InlineData("[mail](mailto:test@example.com)", "href=\"mailto:test@example.com\"")]
    public void Render_preserves_approved_link_urls(string markdown, string expectedAttribute)
    {
        var html = SafeMarkdownRenderer.Render(markdown);

        Assert.Contains(expectedAttribute, html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_removes_mailto_from_image_sources()
    {
        var html = SafeMarkdownRenderer.Render("<img src=\"mailto:test@example.com\" alt=\"unsafe source\">");

        Assert.DoesNotContain("mailto:", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("alt=\"unsafe source\"", html, StringComparison.Ordinal);
    }
}
