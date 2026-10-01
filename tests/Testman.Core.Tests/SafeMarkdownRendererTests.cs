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

    [Theory]
    [InlineData("<p title=\"not allowed\">text</p>", "title=")]
    [InlineData("<a src=\"https://example.com/image.png\">link</a>", "src=")]
    [InlineData("<img src=\"image.png\" colspan=\"2\">", "colspan=")]
    [InlineData("<td href=\"https://example.com\">cell</td>", "href=")]
    public void Render_removes_attributes_from_unapproved_elements(string markdown, string forbiddenAttribute)
    {
        var html = SafeMarkdownRenderer.Render(markdown);

        Assert.DoesNotContain(forbiddenAttribute, html, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("<a href=\"https://example.com\" title=\"Example\">link</a>", "href=", "title=")]
    [InlineData("<img src=\"image.png\" alt=\"Image\" width=\"10\" height=\"20\">", "src=", "alt=")]
    [InlineData("<table><tbody><tr><td colspan=\"2\" rowspan=\"3\">cell</td></tr></tbody></table>", "colspan=", "rowspan=")]
    public void Render_preserves_attributes_on_approved_elements(string markdown, string firstAttribute, string secondAttribute)
    {
        var html = SafeMarkdownRenderer.Render(markdown);

        Assert.Contains(firstAttribute, html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(secondAttribute, html, StringComparison.OrdinalIgnoreCase);
    }
}
