using ClimaOS_Desktop.ViewModels;
namespace ClimaOS_Desktop.Views;
public partial class MainPage : ContentPage
{
    private readonly WeatherViewModel _viewModel;
    private bool _loaded;
    public MainPage(WeatherViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        SizeChanged += OnPageSizeChanged;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.UpdateAdaptiveLayout(Width);
        if (!_viewModel.IsAuthenticated)
            return;
        if (_loaded)
            return;
        _loaded = true;
        await _viewModel.LoadCommand.ExecuteAsync(null);
        await RootLayout.FadeToAsync(1, 300);
    }
    private void OnPageSizeChanged(object? sender, EventArgs e)
    {
        _viewModel.UpdateAdaptiveLayout(Width);
    }
    private async void OnCardTapped(object? sender, TappedEventArgs e)
    {
        var element = (sender as TapGestureRecognizer)?.Parent as VisualElement;
        if (element is null)
            return;
        await element.ScaleToAsync(0.97, 90, Easing.CubicOut);
        await element.ScaleToAsync(1, 120, Easing.CubicIn);
    }
}
