using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CloudSolutions.Genesys.ViewModels;

internal class ResetGenesysIntegrationViewModel
{
    [BindNever]
    public bool IsEnabled { get; set; }
}
