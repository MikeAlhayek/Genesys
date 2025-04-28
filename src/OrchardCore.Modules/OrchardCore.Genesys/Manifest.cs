using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Genesys Integration",
    Author = ManifestConstants.OrchardCoreTeam,
    Website = ManifestConstants.OrchardCoreWebsite,
    Version = ManifestConstants.OrchardCoreVersion,
    Description = "Provides integrations with Genesys platform.",
    Category = "Telephony",
    Dependencies =
    [
        "CrestApps.OrchardCore.SignalR",
        "OrchardCore.OpenId",
        "OrchardCore.Users.ExternalAuthentication",
    ]
)]
