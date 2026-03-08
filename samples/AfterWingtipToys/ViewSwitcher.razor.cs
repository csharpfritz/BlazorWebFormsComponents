using Microsoft.AspNetCore.Components;

namespace WingtipToys
{
    public partial class ViewSwitcher : ComponentBase
    {
        private string CurrentView { get; set; } = "Desktop";
        private string AlternateView { get; set; } = "Mobile";
        private string SwitchUrl { get; set; } = "#";
    }
}
