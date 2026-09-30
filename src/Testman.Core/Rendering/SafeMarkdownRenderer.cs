using Ganss.Xss;
using Markdig;

namespace Testman.Core.Rendering;

public static class SafeMarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private static readonly string[] AllowedTags =
    [
        "h1", "h2", "h3", "h4", "h5", "h6", "p", "br", "hr", "strong", "em", "del",
        "code", "pre", "blockquote", "ul", "ol", "li", "a", "img", "details", "summary",
        "kbd", "sub", "sup", "table", "thead", "tbody", "tr", "th", "td",
    ];

    private static readonly string[] AllowedAttributes =
        ["href", "title", "src", "alt", "width", "height", "colspan", "rowspan"];

    public static string Render(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var sanitizer = CreateSanitizer();
        var generatedHtml = Markdown.ToHtml(markdown, Pipeline);
        return sanitizer.Sanitize(generatedHtml);
    }

    private static HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new HtmlSanitizer
        {
            KeepChildNodes = false,
        };

        sanitizer.AllowedTags.Clear();
        sanitizer.AllowedTags.UnionWith(AllowedTags);
        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedAttributes.UnionWith(AllowedAttributes);
        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.UnionWith(["http", "https", "mailto"]);
        sanitizer.AllowedCssProperties.Clear();
        sanitizer.AllowedAtRules.Clear();
        sanitizer.FilterUrl += (_, eventArgs) =>
        {
            if (eventArgs.Tag.TagName.Equals("IMG", StringComparison.OrdinalIgnoreCase)
                && Uri.TryCreate(eventArgs.OriginalUrl, UriKind.Absolute, out var uri)
                && uri.Scheme.Equals("mailto", StringComparison.OrdinalIgnoreCase))
            {
                eventArgs.SanitizedUrl = null;
            }
        };

        return sanitizer;
    }
}
