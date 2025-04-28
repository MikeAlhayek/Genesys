using OrchardCore.Security.Permissions;

namespace CloudSolutions.Genesys;

internal static class GenesysPermissions
{
    public static Permission ManageGenesysIntegration = new Permission("ManageGenesysIntegration", "Manage Genesys Integration");
}
