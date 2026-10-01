using Microsoft.Extensions.Configuration;

namespace Testman.Web.Tests;

public sealed class LocalhostBindingGuardTests
{
    [Theory]
    [InlineData("http://localhost:5000")]
    [InlineData("https://127.0.0.1:5001")]
    [InlineData("http://[::1]:5000")]
    [InlineData("http://localhost:5000;https://127.0.0.1:5001")]
    public void Validate_accepts_loopback_urls(string urls)
    {
        var configuration = Configuration(("urls", urls));

        LocalhostBindingGuard.Validate(configuration);
    }

    [Theory]
    [InlineData("http://0.0.0.0:5000")]
    [InlineData("http://*:5000")]
    [InlineData("http://example.com:5000")]
    [InlineData("http://localhost:5000;http://0.0.0.0:5001")]
    public void Validate_rejects_urls_that_are_not_loopback(string urls)
    {
        var configuration = Configuration(("urls", urls));

        var exception = Assert.Throws<InvalidOperationException>(() => LocalhostBindingGuard.Validate(configuration));

        Assert.Contains("localhost", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_rejects_a_non_loopback_kestrel_endpoint()
    {
        var configuration = Configuration(("Kestrel:Endpoints:Http:Url", "http://0.0.0.0:5000"));

        Assert.Throws<InvalidOperationException>(() => LocalhostBindingGuard.Validate(configuration));
    }

    [Theory]
    [InlineData("http_ports")]
    [InlineData("https_ports")]
    public void Validate_rejects_port_only_settings_that_bind_all_interfaces(string key)
    {
        var configuration = Configuration((key, "5000"));

        Assert.Throws<InvalidOperationException>(() => LocalhostBindingGuard.Validate(configuration));
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(value =>
                new KeyValuePair<string, string?>(value.Key, value.Value)))
            .Build();
}
