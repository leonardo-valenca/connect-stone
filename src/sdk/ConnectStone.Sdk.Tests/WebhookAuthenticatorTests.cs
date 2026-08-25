using ConnectStone.Sdk.Webhooks;
using Microsoft.Extensions.Options;

namespace ConnectStone.Sdk.Tests;

public class WebhookAuthenticatorTests
{
    private static WebhookAuthenticator CreateAuthenticator(string username = "hookuser", string password = "hookpass") =>
        new(Options.Create(new WebhookAuthenticatorOptions { Username = username, Password = password }));

    [Fact]
    public void IsAuthentic_returns_true_for_matching_basic_credentials()
    {
        var authenticator = CreateAuthenticator();
        var header = "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("hookuser:hookpass"));

        Assert.True(authenticator.IsAuthentic(header));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Bearer sometoken")]
    [InlineData("Basic not-base64!!")]
    public void IsAuthentic_returns_false_for_malformed_or_missing_header(string? header)
    {
        var authenticator = CreateAuthenticator();

        Assert.False(authenticator.IsAuthentic(header));
    }

    [Fact]
    public void IsAuthentic_returns_false_for_wrong_credentials()
    {
        var authenticator = CreateAuthenticator();
        var header = "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("hookuser:wrongpass"));

        Assert.False(authenticator.IsAuthentic(header));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Basic anything")]
    public void IsAuthentic_returns_true_for_any_request_when_no_credentials_configured(string? header)
    {
        var authenticator = new WebhookAuthenticator(Options.Create(new WebhookAuthenticatorOptions()));

        Assert.True(authenticator.IsAuthentic(header));
    }
}
