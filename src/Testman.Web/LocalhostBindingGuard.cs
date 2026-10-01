using Microsoft.Extensions.Configuration;

namespace Testman.Web;

public static class LocalhostBindingGuard
{
    public static void Validate(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        ValidateUrls(configuration["urls"], "urls");

        foreach (var endpoint in configuration.GetSection("Kestrel:Endpoints").GetChildren())
        {
            ValidateUrls(endpoint["Url"], $"Kestrel:Endpoints:{endpoint.Key}:Url");
        }

        RejectPortOnlySetting(configuration, "http_ports");
        RejectPortOnlySetting(configuration, "https_ports");
    }

    private static void ValidateUrls(string? configuredUrls, string settingName)
    {
        if (string.IsNullOrWhiteSpace(configuredUrls))
        {
            return;
        }

        foreach (var configuredUrl in configuredUrls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Uri.TryCreate(configuredUrl, UriKind.Absolute, out var uri) || !uri.IsLoopback)
            {
                throw new InvalidOperationException(
                    $"Web server binding '{configuredUrl}' from '{settingName}' is not allowed. V0.1 must listen on localhost only.");
            }
        }
    }

    private static void RejectPortOnlySetting(IConfiguration configuration, string settingName)
    {
        if (!string.IsNullOrWhiteSpace(configuration[settingName]))
        {
            throw new InvalidOperationException(
                $"Web server setting '{settingName}' is not allowed because it binds all network interfaces. Configure an explicit localhost URL instead.");
        }
    }
}
