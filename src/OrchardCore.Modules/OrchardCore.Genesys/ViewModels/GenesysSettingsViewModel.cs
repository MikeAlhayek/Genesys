using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CloudSolutions.Genesys.ViewModels;

public class GenesysSettingsViewModel
{
    public string Organization { get; set; }

    public string ClientId { get; set; }

    public string ClientSecret { get; set; }

    public string Region { get; set; }

    [BindNever]
    public bool HasSecret { get; set; }
}
