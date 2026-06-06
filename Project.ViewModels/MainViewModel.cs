using System.Collections.ObjectModel;
using System.ComponentModel;
using Project.Models;

namespace Project.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly DashboardViewModel _dashboardViewModel;
    private readonly ItemsViewModel _itemsViewModel;
    private readonly SettingsViewModel _settingsViewModel;

    private string _appTitle = "Project Template";
    private ShellSection? _selectedSection;
    private object? _currentPageViewModel;

    public MainViewModel(
        DashboardViewModel dashboardViewModel,
        ItemsViewModel itemsViewModel,
        SettingsViewModel settingsViewModel)
    {
        _dashboardViewModel = dashboardViewModel;
        _itemsViewModel = itemsViewModel;
        _settingsViewModel = settingsViewModel;
        ToggleThemeCommand = new AsyncRelayCommand(ToggleThemeAsync);

        Sections = new ObservableCollection<ShellSection>
        {
            new("dashboard", "Dashboard", "Start from a concise project overview."),
            new("items", "Items", "Demonstrates list loading, filtering, and commands."),
            new("settings", "Settings", "Shows preference editing and persistence.")
        };

        _settingsViewModel.PropertyChanged += SettingsViewModelOnPropertyChanged;
    }

    public ObservableCollection<ShellSection> Sections { get; }

    public AsyncRelayCommand ToggleThemeCommand { get; }

    public string AppTitle
    {
        get => _appTitle;
        set => SetProperty(ref _appTitle, value);
    }

    public bool IsDarkMode => _settingsViewModel.IsDarkMode;

    public string ThemeToggleGlyph => IsDarkMode ? "☀" : "☾";

    public string ThemeToggleToolTip => IsDarkMode ? "切换到白天模式" : "切换到暗黑模式";

    public ShellSection? SelectedSection
    {
        get => _selectedSection;
        set
        {
            if (!SetProperty(ref _selectedSection, value))
            {
                return;
            }

            CurrentPageViewModel = value?.Key switch
            {
                "dashboard" => _dashboardViewModel,
                "items" => _itemsViewModel,
                "settings" => _settingsViewModel,
                _ => _dashboardViewModel
            };

            if (value is not null)
            {
                _settingsViewModel.LastOpenedSection = value.Key;
            }
        }
    }

    public object? CurrentPageViewModel
    {
        get => _currentPageViewModel;
        set => SetProperty(ref _currentPageViewModel, value);
    }

    public async Task InitializeAsync()
    {
        await _settingsViewModel.LoadAsync();
        AppTitle = _settingsViewModel.AppTitle;

        await _dashboardViewModel.LoadAsync();
        await _itemsViewModel.LoadAsync();

        SelectedSection = Sections.FirstOrDefault(section =>
            string.Equals(section.Key, _settingsViewModel.LastOpenedSection, StringComparison.OrdinalIgnoreCase))
            ?? Sections[0];
    }

    private void SettingsViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.AppTitle))
        {
            AppTitle = _settingsViewModel.AppTitle;
        }

        if (e.PropertyName == nameof(SettingsViewModel.IsDarkMode))
        {
            OnPropertyChanged(nameof(IsDarkMode));
            OnPropertyChanged(nameof(ThemeToggleGlyph));
            OnPropertyChanged(nameof(ThemeToggleToolTip));
        }
    }

    private async Task ToggleThemeAsync()
    {
        await _settingsViewModel.ToggleThemeAsync();
    }
}
