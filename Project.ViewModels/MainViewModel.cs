using Project.Core;
using Project.Models;

namespace Project.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly IAppPreferencesService _appPreferencesService;

    private AppPreferences _preferences = new();
    private string _appTitle = "Modbus RTU 原始帧生成器";
    private bool _isDarkMode;

    public MainViewModel(
        IAppPreferencesService appPreferencesService,
        ModbusCrcViewModel modbusCrcViewModel)
    {
        _appPreferencesService = appPreferencesService;
        ModbusCrcViewModel = modbusCrcViewModel;
        ToggleThemeCommand = new AsyncRelayCommand(ToggleThemeAsync);
    }

    public AsyncRelayCommand ToggleThemeCommand { get; }

    public ModbusCrcViewModel ModbusCrcViewModel { get; }

    public string AppTitle
    {
        get => _appTitle;
        private set => SetProperty(ref _appTitle, value);
    }

    public bool IsDarkMode
    {
        get => _isDarkMode;
        private set
        {
            if (!SetProperty(ref _isDarkMode, value))
            {
                return;
            }

            OnPropertyChanged(nameof(ThemeToggleGlyph));
            OnPropertyChanged(nameof(ThemeToggleToolTip));
        }
    }

    public string ThemeToggleGlyph => IsDarkMode ? "☀" : "☾";

    public string ThemeToggleToolTip => IsDarkMode ? "切换到白天模式" : "切换到暗黑模式";

    public async Task InitializeAsync()
    {
        _preferences = await _appPreferencesService.LoadAsync();
        AppTitle = string.IsNullOrWhiteSpace(_preferences.AppTitle)
            ? "Modbus RTU 原始帧生成器"
            : _preferences.AppTitle;
        IsDarkMode = _preferences.IsDarkMode;
    }

    private async Task ToggleThemeAsync()
    {
        IsDarkMode = !IsDarkMode;
        _preferences.AppTitle = AppTitle;
        _preferences.IsDarkMode = IsDarkMode;
        await _appPreferencesService.SaveAsync(_preferences);
    }
}
