using CloudSolutions.Genesys.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Client;

namespace CloudSolutions.Genesys;

internal sealed class OpenIddictClientOptionsConfiguration : IConfigureOptions<OpenIddictClientOptions>
{
    private readonly GenesysAuthenticationOptions _genesysOptions;
    private readonly ILogger _logger;

    public OpenIddictClientOptionsConfiguration(
        IOptions<GenesysAuthenticationOptions> genesysOptions,
        ILogger<OpenIddictClientOptionsConfiguration> logger)
    {
        _genesysOptions = genesysOptions.Value;
        _logger = logger;
    }

    public void Configure(OpenIddictClientOptions options)
    {
        if (string.IsNullOrEmpty(_genesysOptions.ClientId) || string.IsNullOrEmpty(_genesysOptions.ClientSecret))
        {
            return;
        }

        var conf = new OpenIddict.Abstractions.OpenIddictConfiguration()
        {
            AuthorizationEndpoint = new Uri(_genesysOptions.AuthorizationEndpoint, UriKind.Absolute),
            TokenEndpoint = new Uri(_genesysOptions.TokenEndpoint, UriKind.Absolute),
            UserInfoEndpoint = new Uri(_genesysOptions.UserInformationEndpoint),
            GrantTypesSupported =
            {
                "client_credentials",
                "authorization_code",
                "refresh_token",
            },
            ResponseTypesSupported =
            {
                "code",
            },
        };

        var clientOptions = new OpenIddictClientRegistration
        {
            Configuration = conf,
            ProviderName = GenesysConstants.ProviderName,
            ProviderDisplayName = "Genesys Cloud",
            Issuer = new Uri(_genesysOptions.ClaimsIssuer, UriKind.RelativeOrAbsolute),
            ClientId = _genesysOptions.ClientId,
            ClientSecret = _genesysOptions.ClientSecret,
            RedirectUri = new Uri(_genesysOptions.CallbackPath, UriKind.RelativeOrAbsolute),
            Scopes =
            {
                "user-basic-info",
                "alerting",
                "conversations",
                "presence",
            },
        };

        options.Registrations.Add(clientOptions);
    }
}
