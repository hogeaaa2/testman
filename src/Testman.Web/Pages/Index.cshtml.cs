using Microsoft.AspNetCore.Mvc.RazorPages;
using Testman.Web.Presentation;

namespace Testman.Web.Pages;

public sealed class IndexModel(SpecificationPageContentSource contentSource) : PageModel
{
    public SpecificationPageContent PageContent { get; private set; } = null!;
    public bool IsVerificationMode { get; private set; }

    public void OnGet(string? mode)
    {
        IsVerificationMode = mode == "verification";
        PageContent = contentSource.Load();
    }
}
