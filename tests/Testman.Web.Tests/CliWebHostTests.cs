using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Testman.Core.Persistence;

namespace Testman.Web.Tests;

public sealed class CliWebHostTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"testman-web-{Guid.NewGuid():N}");

    public CliWebHostTests() => Directory.CreateDirectory(directory);

    [Fact]
    public async Task Invalid_arguments_exit_with_an_error_without_starting_the_server()
    {
        using var process = StartProcess(["serve"]);

        try
        {
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));

            Assert.Equal(1, process.ExitCode);
            Assert.Contains("testman serve --specs <path>", process.StandardError.ReadToEnd(), StringComparison.Ordinal);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }
    }

    [Fact]
    public async Task Database_initialization_failure_exits_with_an_error_without_starting_the_server()
    {
        WriteFile("valid.md", ValidSpecification("Valid title"));
        var databaseDirectory = Path.Combine(directory, "database-as-directory");
        Directory.CreateDirectory(databaseDirectory);
        using var process = StartProcess(
            ["serve", "--specs", "valid.md", "--db", databaseDirectory, "--port", FindAvailablePort().ToString()]);

        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
        var error = process.StandardError.ReadToEnd();

        Assert.Equal(1, process.ExitCode);
        Assert.Contains("Database could not be initialized", error, StringComparison.Ordinal);
        Assert.DoesNotContain("Unhandled exception", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Home_page_displays_valid_titles_and_diagnostics_from_the_selected_path()
    {
        WriteFile("valid.md", ValidSpecification("Valid title"));
        WriteFile("invalid.md", "Testman-Format-Version: 1\n\n# Invalid title");
        Git("init");
        Git("config", "user.name", "Test User");
        Git("config", "user.email", "test@example.invalid");
        Git("add", "valid.md", "invalid.md");
        Git("commit", "-m", "Add specifications");
        var port = FindAvailablePort();
        var ignoredEnvironmentPort = FindAvailablePort();
        var relativeDatabasePath = Path.Combine("data", "results.db");
        using var process = StartProcess(
            ["serve", "--specs", ".", "--db", relativeDatabasePath, "--port", port.ToString()],
            new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
                ["Kestrel__Endpoints__Http__Url"] = $"http://localhost:{ignoredEnvironmentPort}",
            });

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
            var html = await GetWhenReady(client, process);

            var databasePath = Path.Combine(directory, relativeDatabasePath);
            Assert.True(File.Exists(databasePath));
            using (var connection = SqliteConnectionFactory.Open(databasePath, pooling: false))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT name FROM schema_migrations WHERE version = 1";
                Assert.Equal("create_result_history", command.ExecuteScalar());
            }

            Assert.Contains("Valid title", html, StringComparison.Ordinal);
            Assert.Contains("Safe overview", html, StringComparison.Ordinal);
            Assert.DoesNotContain("alert('unsafe')", html, StringComparison.Ordinal);
            Assert.Contains("Major", html, StringComparison.Ordinal);
            Assert.Contains("Middle", html, StringComparison.Ordinal);
            Assert.Contains("Minor", html, StringComparison.Ordinal);
            Assert.Contains("invalid.md", html, StringComparison.Ordinal);
            Assert.Matches("valid\\.md\\s+\\([0-9a-f]{40}\\)", html);
            Assert.Contains("Required section", html, StringComparison.Ordinal);
            Assert.DoesNotContain("TC-1", html, StringComparison.Ordinal);
            Assert.Contains("Test patterns", html, StringComparison.Ordinal);
            Assert.Contains("Verification", html, StringComparison.Ordinal);
            Assert.Contains("<details class=\"card border-0 shadow-sm mt-4 specification-card\"", html, StringComparison.Ordinal);
            Assert.Contains(
                $"data-specification-path=\"{WebUtility.HtmlEncode(Path.Combine(directory, "valid.md"))}\"",
                html,
                StringComparison.Ordinal);
            Assert.Contains("<summary class=\"card-header bg-body-tertiary d-flex flex-wrap justify-content-between gap-2\"", html, StringComparison.Ordinal);
            Assert.Contains("class=\"specification-toggle\"", html, StringComparison.Ordinal);
            Assert.Contains("Current test status", html, StringComparison.Ordinal);
            Assert.Contains("Total 1", html, StringComparison.Ordinal);
            Assert.Contains("Not Tested 1", html, StringComparison.Ordinal);
            Assert.Contains("/lib/bootstrap/dist/css/bootstrap.min.", html, StringComparison.Ordinal);
            Assert.DoesNotContain("cdn.", html, StringComparison.OrdinalIgnoreCase);

            var siteScript = await client.GetStringAsync("/js/site.js");
            Assert.Contains("sessionStorage.getItem", siteScript, StringComparison.Ordinal);
            Assert.Contains("sessionStorage.setItem", siteScript, StringComparison.Ordinal);
            Assert.Contains("addEventListener(\"toggle\"", siteScript, StringComparison.Ordinal);
            Assert.Contains("data-specification-path", siteScript, StringComparison.Ordinal);

            var specification = GitSpecificationReference.Resolve(Path.Combine(directory, "valid.md"));
            var store = new ResultHistoryStore(databasePath);
            store.Append(new ResultSubmission(
                new DateTimeOffset(2026, 10, 2, 3, 4, 5, TimeSpan.Zero),
                "<script>tester</script>",
                [new TestResultInput(
                    specification,
                    "TC-1",
                    TestResultOutcome.Pass,
                    "<img src=x onerror=alert('history')>")]));

            WriteFile("valid.md", ValidSpecification("Updated title"));
            var updatedHtml = await client.GetStringAsync("/");

            Assert.Contains("Updated title", updatedHtml, StringComparison.Ordinal);
            Assert.DoesNotContain("Valid title", updatedHtml, StringComparison.Ordinal);

            var verificationHtml = await client.GetStringAsync("/?mode=verification");

            Assert.Contains("TC-1", verificationHtml, StringComparison.Ordinal);
            Assert.True(
                Regex.Matches(verificationHtml, "valid\\.md\\s+\\([0-9a-f]{40}\\)").Count >= 2,
                "The Test list and specification card should both show the file's last committed revision.");
            Assert.Contains("Test list", verificationHtml, StringComparison.Ordinal);
            Assert.Contains("class=\"card border-0 shadow-sm mt-4 specification-card test-list-card\" open", verificationHtml, StringComparison.Ordinal);
            Assert.Matches("Specification file</th>\\s*<th scope=\"col\">Pass</th>\\s*<th scope=\"col\">Fail</th>\\s*<th scope=\"col\">Blocked</th>\\s*<th scope=\"col\">N/A</th>\\s*<th scope=\"col\">Not Tested</th>\\s*<th scope=\"col\">Total</th>", verificationHtml);
            Assert.DoesNotContain("Latest result", verificationHtml, StringComparison.Ordinal);
            Assert.DoesNotContain("Last executed", verificationHtml, StringComparison.Ordinal);
            Assert.Contains("Verification precondition", verificationHtml, StringComparison.Ordinal);
            Assert.Contains("Shared step", verificationHtml, StringComparison.Ordinal);
            Assert.Contains("<td class=\"markdown-content test-steps\"><ol>", verificationHtml, StringComparison.Ordinal);
            Assert.Contains("<li>Case step<br>2. Follow-up step</li>", verificationHtml, StringComparison.Ordinal);
            Assert.Contains("Expected success", verificationHtml, StringComparison.Ordinal);
            Assert.Contains("Previous result", verificationHtml, StringComparison.Ordinal);
            Assert.Matches(
                "Result input</th>\\s*<th scope=\"col\">Optional comment</th>",
                verificationHtml);
            Assert.Matches(
                "</select>\\s*</td>\\s*<td>\\s*<label[^>]+>Comment for TC-1</label>\\s*<textarea",
                verificationHtml);
            Assert.Contains("Pass", verificationHtml, StringComparison.Ordinal);
            Assert.Contains("History (1)", verificationHtml, StringComparison.Ordinal);
            Assert.Contains("Pass 1", verificationHtml, StringComparison.Ordinal);
            Assert.Contains("Not Tested 0", verificationHtml, StringComparison.Ordinal);
            Assert.Contains("&lt;script&gt;tester&lt;/script&gt;", verificationHtml, StringComparison.Ordinal);
            Assert.Contains("&lt;img src=x onerror=alert", verificationHtml, StringComparison.Ordinal);
            Assert.DoesNotContain("<script>tester</script>", verificationHtml, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("<img src=x", verificationHtml, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Safe overview", verificationHtml, StringComparison.Ordinal);
        }
        finally
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }
    }

    [Fact]
    public async Task Home_page_starts_and_displays_a_diagnostic_when_the_file_has_no_title_block()
    {
        WriteFile("empty.md", "Testman-Format-Version: 1");
        var port = FindAvailablePort();
        using var process = StartProcess(
            ["serve", "--specs", "empty.md", "--db", "results.db", "--port", port.ToString()]);

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
            var html = await GetWhenReady(client, process);

            Assert.Contains("empty.md", html, StringComparison.Ordinal);
            Assert.Contains("A title block is required.", html, StringComparison.Ordinal);
            Assert.Contains("Needs attention", html, StringComparison.Ordinal);
            Assert.DoesNotContain(">Ready<", html, StringComparison.Ordinal);
        }
        finally
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }
    }

    [Fact]
    public async Task Home_page_keeps_a_non_git_specification_visible_as_not_tested()
    {
        WriteFile("valid.md", ValidSpecification("Non Git title"));
        var port = FindAvailablePort();
        using var process = StartProcess(
            ["serve", "--specs", "valid.md", "--db", "results.db", "--port", port.ToString()]);

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
            var html = await GetWhenReady(client, process, "/?mode=verification");

            Assert.Contains("Non Git title", html, StringComparison.Ordinal);
            Assert.Contains("Not Tested", html, StringComparison.Ordinal);
            Assert.DoesNotContain("Current test status", html, StringComparison.Ordinal);
        }
        finally
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }
    }

    [Fact]
    public async Task Runtime_database_read_failure_returns_an_error_instead_of_not_tested()
    {
        WriteFile("valid.md", ValidSpecification("Database failure"));
        Git("init");
        Git("config", "user.name", "Test User");
        Git("config", "user.email", "test@example.invalid");
        Git("add", "valid.md");
        Git("commit", "-m", "Add specification");
        var databasePath = Path.Combine(directory, "results.db");
        var port = FindAvailablePort();
        using var process = StartProcess(
            ["serve", "--specs", "valid.md", "--db", databasePath, "--port", port.ToString()]);

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
            await GetWhenReady(client, process);
            File.Move(databasePath, $"{databasePath}.moved");
            Directory.CreateDirectory(databasePath);

            using var response = await client.GetAsync("/?mode=verification");
            var html = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.DoesNotContain("Not Tested", html, StringComparison.Ordinal);
            Assert.DoesNotContain("Microsoft.Data.Sqlite", html, StringComparison.Ordinal);
        }
        finally
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }
    }

    [Fact]
    public async Task Verification_form_confirms_partial_selection_then_redirects_after_saving()
    {
        var specification = ValidSpecification("Submit results").Replace(
            "| TC-1 | Major | Middle | Minor | 1. Case step<br>2. Follow-up step | Expected success |",
            "| TC-1 | Major | Middle | Minor | 1. Case step<br>2. Follow-up step | Expected success |\n| TC-2 | Major | Middle | Other | - | Other success |",
            StringComparison.Ordinal);
        WriteFile("valid.md", specification);
        Git("init");
        Git("config", "user.name", "Test User");
        Git("config", "user.email", "test@example.invalid");
        Git("add", "valid.md");
        Git("commit", "-m", "Add specification");
        var databasePath = Path.Combine(directory, "results.db");
        var port = FindAvailablePort();
        using var process = StartProcess(
            ["serve", "--specs", "valid.md", "--db", databasePath, "--port", port.ToString()]);

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
            var page = await GetWhenReady(client, process, "/?mode=verification");
            var submissionAction = SubmissionAction(page);
            Assert.EndsWith("#result-submission", submissionAction, StringComparison.Ordinal);
            var values = SubmissionValues(page);

            using var confirmationResponse = await client.PostAsync(submissionAction, new FormUrlEncodedContent(values));
            var confirmationHtml = await confirmationResponse.Content.ReadAsStringAsync();
            Assert.Contains("Not all test cases have a result", confirmationHtml, StringComparison.Ordinal);
            AssertFeedbackNearSubmission(
                confirmationHtml,
                "Not all test cases have a result");
            Assert.Equal(0L, ResultCount(databasePath));

            values = SubmissionValues(confirmationHtml);
            submissionAction = SubmissionAction(confirmationHtml);
            using var savedResponse = await client.PostAsync(submissionAction, new FormUrlEncodedContent(values));
            var savedHtml = await savedResponse.Content.ReadAsStringAsync();
            Assert.Contains("Results saved.", savedHtml, StringComparison.Ordinal);
            AssertFeedbackNearSubmission(savedHtml, "Results saved.");
            Assert.Equal("#result-submission", savedResponse.RequestMessage?.RequestUri?.Fragment);
            Assert.Contains("History (1)", savedHtml, StringComparison.Ordinal);
            Assert.Equal(1L, ResultCount(databasePath));
        }
        finally
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }
    }

    public void Dispose()
    {
        foreach (var entry in new DirectoryInfo(directory).EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
        {
            entry.Attributes = FileAttributes.Normal;
        }

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                Directory.Delete(directory, recursive: true);
                break;
            }
            catch (IOException) when (attempt < 4)
            {
                Thread.Sleep(50);
            }
        }
    }

    private Process StartProcess(
        IReadOnlyList<string> arguments,
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = directory,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        startInfo.ArgumentList.Add(typeof(Program).Assembly.Location);
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (environment is not null)
        {
            foreach (var pair in environment)
            {
                startInfo.Environment[pair.Key] = pair.Value;
            }
        }

        return Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start the Testman Web process.");
    }

    private static async Task<string> GetWhenReady(HttpClient client, Process process, string path = "/")
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(15);
        Exception? lastException = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException($"Testman Web exited with code {process.ExitCode}.");
            }

            try
            {
                return await client.GetStringAsync(path);
            }
            catch (HttpRequestException exception)
            {
                lastException = exception;
                await Task.Delay(100);
            }
        }

        throw new TimeoutException("Testman Web did not become ready.", lastException);
    }

    private static int FindAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private void WriteFile(string relativePath, string content) =>
        File.WriteAllText(Path.Combine(directory, relativePath), content, new UTF8Encoding(false));

    private void Git(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = directory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo)!;
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, error);
    }

    private Dictionary<string, string> SubmissionValues(string html)
    {
        var token = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
            RegexOptions.CultureInvariant).Groups[1].Value;
        Assert.NotEmpty(token);
        var confirmPartial = Regex.Match(
            html,
            "name=\"ConfirmPartial\"[^>]*value=\"([^\"]+)\"",
            RegexOptions.CultureInvariant).Groups[1].Value;
        Assert.NotEmpty(confirmPartial);
        return new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token),
            ["ExecutedBy"] = "Tester",
            ["ConfirmPartial"] = confirmPartial,
            ["ResultCases[0].SourcePath"] = Path.Combine(directory, "valid.md"),
            ["ResultCases[0].TestCaseId"] = "TC-1",
            ["ResultCases[0].Outcome"] = "pass",
            ["ResultCases[0].Comment"] = "works",
            ["ResultCases[1].SourcePath"] = Path.Combine(directory, "valid.md"),
            ["ResultCases[1].TestCaseId"] = "TC-2",
            ["ResultCases[1].Outcome"] = string.Empty,
            ["ResultCases[1].Comment"] = string.Empty,
        };
    }

    private static string SubmissionAction(string html)
    {
        var action = Regex.Match(
            html,
            "<form[^>]*action=\"([^\"]+)\"[^>]*>",
            RegexOptions.CultureInvariant).Groups[1].Value;
        Assert.NotEmpty(action);
        return WebUtility.HtmlDecode(action);
    }

    private static long ResultCount(string databasePath)
    {
        using var connection = SqliteConnectionFactory.Open(databasePath, pooling: false);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM test_results";
        return (long)command.ExecuteScalar()!;
    }

    private static void AssertFeedbackNearSubmission(string html, string message)
    {
        var feedbackIndex = html.IndexOf(message, StringComparison.Ordinal);
        Assert.True(feedbackIndex > html.LastIndexOf("Optional comment", StringComparison.Ordinal));
        Assert.True(feedbackIndex < html.IndexOf("Executed by", StringComparison.Ordinal));
    }

    private static string ValidSpecification(string title) => $$"""
        Testman-Format-Version: 1

        # {{title}}

        ## Overview

        Safe overview<script>alert('unsafe')</script>

        ## Preconditions

        Verification precondition

        ## Common steps

        Shared step

        | ID | Major item | Middle item | Minor item | Steps | Expected result |
        |---|---|---|---|---|---|
        | TC-1 | Major | Middle | Minor | 1. Case step<br>2. Follow-up step | Expected success |
        """;
}
