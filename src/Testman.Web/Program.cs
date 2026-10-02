using Testman.Core.Commands;
using Testman.Core.Persistence;
using Testman.Web.Presentation;
using Microsoft.Data.Sqlite;

var workingDirectory = Environment.CurrentDirectory;
var startup = ServeStartup.Prepare(args, workingDirectory);
if (!startup.CanStart)
{
    Console.Error.WriteLine(startup.Error);
    return 1;
}

try
{
    var databasePath = Path.GetFullPath(startup.Command!.DatabasePath, workingDirectory);
    DatabaseMigrationRunner.Apply(databasePath);
}
catch (Exception exception) when (exception is ArgumentException
    or NotSupportedException
    or IOException
    or UnauthorizedAccessException
    or SqliteException)
{
    Console.Error.WriteLine($"Database could not be initialized: {exception.Message}");
    return 1;
}

var builder = WebApplication.CreateBuilder([]);
builder.Configuration.Sources.Clear();
builder.Configuration.AddInMemoryCollection();
Testman.Web.LocalhostBindingGuard.Validate(builder.Configuration);
builder.WebHost.UseUrls($"http://localhost:{startup.Command.Port}");

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSingleton(new SpecificationPageContentSource(
    startup.Command.SpecificationPath,
    workingDirectory));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

await app.RunAsync();
return 0;

public partial class Program;
