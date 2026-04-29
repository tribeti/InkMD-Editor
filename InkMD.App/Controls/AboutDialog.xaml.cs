using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel;

namespace InkMD_Editor.Controls;

public sealed partial class AboutDialog : ContentDialog
{
    public string? AppVersionString { get; }
    public AboutDialog()
    {
        InitializeComponent();

        PackageVersion version = Package.Current.Id.Version;
        AppVersionString = $"ver {version.Major}.{version.Minor}.{version.Build}";
    }
}
