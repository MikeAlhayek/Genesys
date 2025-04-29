using System.Diagnostics;
using System.Security.Cryptography;
using CloudSolutions.Genesys;
using CloudSolutions.Genesys.Drivers;
using CloudSolutions.Genesys.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Client;
using OpenIddict.Client.AspNetCore;
using OrchardCore.Settings;

namespace OrchardCore.Genesys;

internal sealed class OpenIddictClientOptionsConfiguration
    : IConfigureOptions<OpenIddictClientOptions>, IConfigureNamedOptions<OpenIddictClientAspNetCoreOptions>
{
    private readonly ISiteService _siteService;
    private readonly IDataProtectionProvider _dataProtectionProvider;

    public OpenIddictClientOptionsConfiguration(
        ISiteService siteService,
        IDataProtectionProvider dataProtectionProvider)
    {
        _siteService = siteService;
        _dataProtectionProvider = dataProtectionProvider;
    }

    public void Configure(OpenIddictClientOptions options)
    {
        var settings = _siteService.GetSettings<GenesysSettings>();

        if (string.IsNullOrEmpty(settings.ClientId) || string.IsNullOrEmpty(settings.ClientSecret) || settings.Authority is null)
        {
            if (options.RedirectionEndpointUris.Count == 0)
            {
                options.RedirectionEndpointUris.Add(new Uri(GenesysConstants.DefaultSignIn, UriKind.Relative));
            }

            return;
        }

        var conf = new OpenIddict.Abstractions.OpenIddictConfiguration()
        {
            AuthorizationEndpoint = new Uri($"{settings.Authority.Scheme}://login.{settings.Authority.Host}/oauth/authorize", UriKind.Absolute),
            TokenEndpoint = new Uri($"{settings.Authority.Scheme}://login.{settings.Authority.Host}/oauth/token", UriKind.Absolute),
            UserInfoEndpoint = new Uri($"{settings.Authority.Scheme}://api.{settings.Authority.Host}/api/v2/users/me"),
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

        var registration = new OpenIddictClientRegistration
        {
            Configuration = conf,
            ProviderName = GenesysConstants.ProviderName,
            ProviderDisplayName = settings.DisplayName ?? "Genesys Cloud",
            Issuer = settings.Authority,
            ClientId = settings.ClientId,
            RedirectUri = new Uri(settings.CallbackPath ?? GenesysConstants.DefaultSignIn, UriKind.RelativeOrAbsolute),
            PostLogoutRedirectUri = new Uri(settings.SignedOutCallbackPath ?? GenesysConstants.DefaultSignOut, UriKind.RelativeOrAbsolute),
            Scopes =
            {
                "user-basic-info",
            },
            Properties =
            {
                [nameof(GenesysSettings)] = settings,
            },
        };

        /*
        if (!string.IsNullOrEmpty(settings.ResponseMode))
        {
            registration.ResponseModes.Add(settings.ResponseMode);
        }
        */

        if (!string.IsNullOrEmpty(settings.ResponseType))
        {
            registration.ResponseTypes.Add(settings.ResponseType);
        }

        if (settings.Scopes != null)
        {
            registration.Scopes.UnionWith(settings.Scopes);
        }

        if (!string.IsNullOrEmpty(settings.ClientSecret))
        {
            var protector = _dataProtectionProvider.CreateProtector(GenesysSettingsDisplayDriver.GenesysProtectorName);

            registration.ClientSecret = protector.Unprotect(settings.ClientSecret);
        }

        // Note: claims are mapped by CallbackController, so the built-in mapping feature is unnecessary.
        // options.DisableWebServicesFederationClaimMapping = true;

        // TODO: use proper encryption/signing credentials, similar to what's used for the server feature.
        options.EncryptionCredentials.Add(new EncryptingCredentials(new SymmetricSecurityKey(
            RandomNumberGenerator.GetBytes(256 / 8)), SecurityAlgorithms.Aes256KW, SecurityAlgorithms.Aes256CbcHmacSha512));

        options.SigningCredentials.Add(new SigningCredentials(new SymmetricSecurityKey(
            RandomNumberGenerator.GetBytes(256 / 8)), SecurityAlgorithms.HmacSha256));

        options.Registrations.Add(registration);
    }

    public void Configure(string name, OpenIddictClientAspNetCoreOptions options)
    {
        // Note: the OpenID module handles the redirection requests in its dedicated
        // ASP.NET Core MVC controller, which requires enabling the pass-through mode.
        options.EnableRedirectionEndpointPassthrough = true;
        options.EnablePostLogoutRedirectionEndpointPassthrough = true;

        // Note: error pass-through is enabled to allow the actions of the MVC callback controller
        // to handle the errors returned by the interactive endpoints without relying on the generic
        // status code pages middleware to rewrite the response later in the request processing.
        options.EnableErrorPassthrough = true;

        // Note: in Orchard, transport security is usually configured via the dedicated HTTPS module.
        // To make configuration easier and avoid having to configure it in two different features,
        // the transport security requirement enforced by OpenIddict by default is always turned off.
        options.DisableTransportSecurityRequirement = true;
    }

    public void Configure(OpenIddictClientAspNetCoreOptions options)
        => Debug.Fail("This infrastructure method shouldn't be called.");
}
