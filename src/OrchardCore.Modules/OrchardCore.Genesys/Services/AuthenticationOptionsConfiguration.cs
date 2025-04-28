using CloudSolutions.Genesys.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Client.AspNetCore;
using OrchardCore.Settings;

namespace CloudSolutions.Genesys.Services;

internal sealed class AuthenticationOptionsConfiguration : IConfigureOptions<AuthenticationOptions>
{
    private readonly ISiteService _siteService;
    private readonly ILogger _logger;

    public AuthenticationOptionsConfiguration(
        ISiteService siteService,
        ILogger<AuthenticationOptionsConfiguration> logger)
    {
        _siteService = siteService;
        _logger = logger;
    }

    public void Configure(AuthenticationOptions options)
    {
        var settings = _siteService.GetSettings<GenesysSettings>();

        if (string.IsNullOrWhiteSpace(settings.ClientId) || string.IsNullOrWhiteSpace(settings.ClientSecret))
        {
            _logger.LogWarning("The Genesys login provider is enabled but not configured.");

            return;
        }

        options.AddScheme<OpenIddictClientAspNetCoreForwarder>(GenesysConstants.ProviderName, "Genesys Cloud");
    }
}
