using CloudSolutions.Genesys.Drivers;
using CloudSolutions.Genesys.Models;
using CloudSolutions.Genesys.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Client;
using OpenIddict.Client.AspNetCore;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.OpenId.Settings;
using OrchardCore.Settings;

namespace CloudSolutions.Genesys;

public class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSiteDisplayDriver<GenesysSettingsDisplayDriver>();
        services.AddNavigationProvider<AdminMenu>();

        services
           .AddHttpClient()
           .AddOpenIddict()
           // Register the OpenIddict client components.
           .AddClient(options =>
           {
               options.UseAspNetCore();
               options.UseSystemNetHttp();

               // TODO: determine what flows we want to enable and whether this
               // should be configurable by the user (like the server feature).
               options.AllowAuthorizationCodeFlow()
                      .AllowHybridFlow()
                      .AllowImplicitFlow();

               options.AddEventHandler<OpenIddictClientEvents.ProcessChallengeContext>(builder =>
               {
                   builder.UseInlineHandler(static context =>
                   {
                       // If the client registration is managed by Orchard, attach the custom parameters set by the user.
                       if (context.Registration.Properties.TryGetValue(nameof(OpenIdClientSettings), out var value) &&
                           value is OpenIdClientSettings settings && settings.Parameters is { Length: > 0 } parameters)
                       {
                           foreach (var parameter in parameters)
                           {
                               context.Parameters[parameter.Name] = parameter.Value;
                           }
                       }

                       return default;
                   });

                   builder.SetOrder(OpenIddictClientHandlers.AttachCustomChallengeParameters.Descriptor.Order - 1);
               });
           });

        services.AddTransient<IConfigureOptions<OpenIddictClientOptions>, OpenIddictClientOptionsConfiguration>();

        services.AddTransient<IConfigureOptions<OpenIddictClientAspNetCoreOptions>, OpenIddictClientOptionsConfiguration>();
    }

    public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
    {
        var siteService = serviceProvider.GetRequiredService<ISiteService>();

        var settings = siteService.GetSettings<GenesysSettings>();

        routes.MapAreaControllerRoute(
            name: "OpenIdCallback.LogInCallback",
            areaName: typeof(Startup).Namespace,
            pattern: settings?.CallbackPath ?? "signin-genesys",
            defaults: new { controller = "Callback", action = "LogInCallback" }
        );

        routes.MapAreaControllerRoute(
            name: "OpenIdCallback.LogOutCallback",
            areaName: typeof(Startup).Namespace,
            pattern: settings?.SignedOutCallbackPath ?? "signout-callback-genesys",
            defaults: new { controller = "Callback", action = "LogOutCallback" }
        );
    }
}
