using AngleSharp.Html.Parser;
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

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> AllowedAttributesByTag =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["a"] = new HashSet<string>(["href", "title"], StringComparer.OrdinalIgnoreCase),
            ["img"] = new HashSet<string>(["src", "alt", "title", "width", "height"], StringComparer.OrdinalIgnoreCase),
            ["table"] = TableAttributes(),
            ["thead"] = TableAttributes(),
            ["tbody"] = TableAttributes(),
            ["tr"] = TableAttributes(),
            ["th"] = TableAttributes(),
            ["td"] = TableAttributes(),
        };

    public static string Render(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var sanitizer = CreateSanitizer();
        var generatedHtml = Markdown.ToHtml(markdown, Pipeline);
        return FilterAttributesByTag(sanitizer.Sanitize(generatedHtml));
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

    private static string FilterAttributesByTag(string html)
    {
        var document = new HtmlParser().ParseDocument($"<body>{html}</body>");

        foreach (var element in document.Body!.QuerySelectorAll("*"))
        {
            AllowedAttributesByTag.TryGetValue(element.LocalName, out var allowedAttributes);
            foreach (var attribute in element.Attributes.ToArray())
            {
                if (allowedAttributes is null || !allowedAttributes.Contains(attribute.LocalName))
                {
                    element.RemoveAttribute(attribute.LocalName);
                }
            }
        }

        return document.Body.InnerHtml;
    }

    private static IReadOnlySet<string> TableAttributes() =>
        new HashSet<string>(["colspan", "rowspan"], StringComparer.OrdinalIgnoreCase);
}
