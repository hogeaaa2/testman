using Testman.Core.Commands;
using Testman.Web.Presentation;

var startup = ServeStartup.Prepare(args, Environment.CurrentDirectory);
if (!startup.CanStart)
{
    Console.Error.WriteLine(startup.Error);
    return 1;
}

var builder = WebApplication.CreateBuilder([]);
Testman.Web.LocalhostBindingGuard.Validate(builder.Configuration);
builder.WebHost.UseUrls($"http://localhost:{startup.Command!.Port}");

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSingleton(new SpecificationPageContentSource(
    startup.Command.SpecificationPath,
    Environment.CurrentDirectory));

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
