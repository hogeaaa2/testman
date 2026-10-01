using Microsoft.AspNetCore.Mvc.RazorPages;
using Testman.Web.Presentation;

namespace Testman.Web.Pages;

public sealed class IndexModel(SpecificationPageContent content) : PageModel
{
    public SpecificationPageContent PageContent { get; } = content;

    public void OnGet() { }
}
