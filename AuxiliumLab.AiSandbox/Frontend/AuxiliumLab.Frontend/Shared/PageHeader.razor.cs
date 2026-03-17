using Microsoft.AspNetCore.Components;

namespace AuxiliumLab.Frontend.Shared;

public partial class PageHeader
{
    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;
}
