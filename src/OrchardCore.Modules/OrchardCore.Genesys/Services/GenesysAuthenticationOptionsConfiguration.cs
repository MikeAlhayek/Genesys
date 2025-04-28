using CloudSolutions.Genesys.Drivers;
using CloudSolutions.Genesys.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using OrchardCore.Settings;
using PureCloudPlatform.Client.V2.Client;

namespace CloudSolutions.Genesys.Services;

internal sealed class GenesysAuthenticationOptionsConfiguration : IPostConfigureOptions<GenesysAuthenticationOptions>
{
    private readonly ISiteService _siteService;
    private readonly IDataProtectionProvider _dataProtectionProvider;

    public GenesysAuthenticationOptionsConfiguration(
        ISiteService siteService,
        IDataProtectionProvider dataProtectionProvider)
    {
        _siteService = siteService;
        _dataProtectionProvider = dataProtectionProvider;
    }

    public void PostConfigure(string name, GenesysAuthenticationOptions options)
    {
        var settings = _siteService.GetSettings<GenesysSettings>();

        options.Region = settings.Region;
        options.UsePkce = true;
        options.CallbackPath = new PathString("/signin-" + GenesysConstants.ProviderName);

        if (string.IsNullOrEmpty(settings.Region) || !Enum.TryParse<PureCloudRegionHosts>(settings.Region, out var region))
        {
            return;
        }

        options.Organization = settings.Organization;
        options.ClientId = settings.ClientId;

        var address = new Uri(region.GetDescription());
        var issuer = new Uri($"{address.Scheme}://{address.Host.Substring(4)}");

        if (!string.IsNullOrEmpty(settings.ClientSecret))
        {
            var protector = _dataProtectionProvider.CreateProtector(GenesysSettingsDisplayDriver.GenesysProtectorName);

            options.ClientSecret = protector.Unprotect(settings.ClientSecret);
        }


        options.ClaimsIssuer = issuer.ToString();
        options.AuthorizationEndpoint = $"{issuer.Scheme}://login.{issuer.Host}/oauth/authorize";
        options.TokenEndpoint = $"{issuer.Scheme}://login.{issuer.Host}/oauth/token";
        options.UserInformationEndpoint = $"{issuer.Scheme}://api.{issuer.Host}/api/v2/users/me";
    }
}
