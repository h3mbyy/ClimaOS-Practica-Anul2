using CommunityToolkit.Mvvm.ComponentModel;

namespace ClimaOS_Desktop.ViewModels;

public partial class NavItemViewModel : ObservableObject
{
    public NavItemViewModel(string key, string title, string icon)
    {
        Key = key;
        Title = title;
        Icon = icon;
    }

    public string Key { get; }
    public string Title { get; }
    public string Icon { get; }

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private bool isDestructive;
}
