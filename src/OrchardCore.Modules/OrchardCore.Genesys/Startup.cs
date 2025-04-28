using CloudSolutions.Genesys.Drivers;
using CloudSolutions.Genesys.Models;
using CloudSolutions.Genesys.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Client;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.Modules;
using OrchardCore.Navigation;

namespace CloudSolutions.Genesys;

public class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSiteDisplayDriver<GenesysSettingsDisplayDriver>();
        services.AddSingleton<GenesysNotificationChannelService>();
        // services.AddSingleton<IBackgroundTask, GenesysNotificationBackgroundService>();
        services.AddNavigationProvider<AdminMenu>();

        services
           .AddHttpClient()
           .AddOpenIddict()
           // Register the OpenIddict client components.
           .AddClient(options =>
           {
               // Allow grant_type=client_credentials to be negotiated.
               options.AllowClientCredentialsFlow();

               options.AllowAuthorizationCodeFlow();

               options.UseAspNetCore();

               options.AddDevelopmentEncryptionCertificate();
               options.AddDevelopmentSigningCertificate();

               // Register the System.Net.Http integration.
               options.UseSystemNetHttp();
           });

        services.AddTransient<IConfigureOptions<OpenIddictClientOptions>, OpenIddictClientOptionsConfiguration>();

        // services.AddTransient<IConfigureOptions<AuthenticationOptions>, AuthenticationOptionsConfiguration>();
        services.AddTransient<IPostConfigureOptions<GenesysAuthenticationOptions>, GenesysAuthenticationOptionsConfiguration>();

        // services.AddTransient<IPostConfigureOptions<GenesysAuthenticationOptions>, OAuthPostConfigureOptions<GenesysAuthenticationOptions, OpenIddictClientAspNetCoreHandler>>();
    }
}
