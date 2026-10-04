using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Testman.Web.Presentation;

namespace Testman.Web.Pages;

public sealed class IndexModel(
    SpecificationPageContentSource contentSource,
    ResultSubmissionCoordinator submissionCoordinator) : PageModel
{
    public SpecificationPageContent PageContent { get; private set; } = null!;
    public bool IsVerificationMode { get; private set; }
    public string? ConfirmationMessage { get; private set; }
    public string? SystemErrorMessage { get; private set; }

    [BindProperty]
    public string ExecutedBy { get; set; } = string.Empty;

    [BindProperty]
    public string TestTargetName { get; set; } = string.Empty;

    [BindProperty]
    public List<ResultCaseForm> ResultCases { get; set; } = [];

    [BindProperty]
    public bool ConfirmPartial { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    public void OnGet(string? mode)
    {
        IsVerificationMode = mode == "verification";
        PageContent = contentSource.Load();
        if (IsVerificationMode)
        {
            ResultCases = CurrentCases();
        }
    }

    public IActionResult OnPost()
    {
        IsVerificationMode = true;
        PageContent = contentSource.Load();

        try
        {
            var response = submissionCoordinator.Submit(new ResultSubmissionRequest(
                ExecutedBy,
                TestTargetName,
                ResultCases.Select(item => new ResultCaseInput(
                    item.SourcePath,
                    item.TestCaseId,
                    item.Outcome,
                    item.Comment)).ToList(),
                ConfirmPartial));

            if (response.Status == ResultSubmissionStatus.Saved)
            {
                SuccessMessage = response.Message;
                return RedirectToPage(
                    pageName: null,
                    pageHandler: null,
                    routeValues: new { mode = "verification" },
                    fragment: "result-submission");
            }

            if (response.Status == ResultSubmissionStatus.NeedsPartialConfirmation)
            {
                ConfirmationMessage = response.Message;
                ConfirmPartial = true;
                ModelState.Remove(nameof(ConfirmPartial));
                return Page();
            }

            ModelState.AddModelError(string.Empty, response.Message);
            return Page();
        }
        catch (SqliteException)
        {
            SystemErrorMessage = "Results could not be saved because the database is unavailable.";
            return Page();
        }
    }

    private List<ResultCaseForm> CurrentCases() => PageContent.Files
        .SelectMany(file => file.Titles.SelectMany(title => title.VerificationCases
            .Where(testCase => testCase.CanRegisterResult)
            .Select(testCase => new ResultCaseForm(file.SourcePath, testCase.Id))))
        .ToList();
}

public sealed class ResultCaseForm(string sourcePath, string testCaseId)
{
    public ResultCaseForm() : this(string.Empty, string.Empty) { }

    public string SourcePath { get; set; } = sourcePath;
    public string TestCaseId { get; set; } = testCaseId;
    public string? Outcome { get; set; }
    public string? Comment { get; set; }
}
