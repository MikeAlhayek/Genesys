using CloudSolutions.Genesys.Drivers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace CloudSolutions.Genesys.Services;

internal sealed class AdminMenu : AdminNavigationProvider
{
    private static readonly RouteValueDictionary _routeValues = new()
    {
        { "area", "OrchardCore.Settings" },
        { "groupId", GenesysSettingsDisplayDriver.GroupId },
    };


    internal readonly IStringLocalizer S;

    public AdminMenu(IStringLocalizer<AdminMenu> stringLocalizer)
    {
        S = stringLocalizer;
    }

    protected override ValueTask BuildAsync(NavigationBuilder builder)
    {
        builder
            .Add(S["Settings"], settings => settings
                .Add(S["Genesys"], S["Genesys"].PrefixPosition(), agentTraining => agentTraining
                    .Action("Index", "Admin", _routeValues)
                    .Permission(GenesysPermissions.ManageGenesysIntegration)
                    .LocalNav()
                )
            );

        return ValueTask.CompletedTask;
    }
}
