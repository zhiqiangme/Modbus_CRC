using Project.Core;
using Project.Models;

namespace Project.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly IAppPreferencesService _appPreferencesService;

    private string _appTitle = "Project Template";
    private string _preferredAccentColor = "#0F766E";
    private bool _isDarkMode;
    private bool _rememberWindowBounds = true;
    private string _lastOpenedSection = "dashboard";
    private string _statusMessage = "Preferences have not been saved yet.";

    public SettingsViewModel(IAppPreferencesService appPreferencesService)
    {
        _appPreferencesService = appPreferencesService;
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        ResetCommand = new RelayCommand(ResetToDefaults);
    }

    public AsyncRelayCommand SaveCommand { get; }

    public RelayCommand ResetCommand { get; }

    public string AppTitle
    {
        get => _appTitle;
        set => SetProperty(ref _appTitle, value);
    }

    public string PreferredAccentColor
    {
        get => _preferredAccentColor;
        set => SetProperty(ref _preferredAccentColor, value);
    }

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set => SetProperty(ref _isDarkMode, value);
    }

    public bool RememberWindowBounds
    {
        get => _rememberWindowBounds;
        set => SetProperty(ref _rememberWindowBounds, value);
    }

    public string LastOpenedSection
    {
        get => _lastOpenedSection;
        set => SetProperty(ref _lastOpenedSection, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public async Task LoadAsync()
    {
        var preferences = await _appPreferencesService.LoadAsync();

        AppTitle = preferences.AppTitle;
        PreferredAccentColor = preferences.PreferredAccentColor;
        IsDarkMode = preferences.IsDarkMode;
        RememberWindowBounds = preferences.RememberWindowBounds;
        LastOpenedSection = preferences.LastOpenedSection;
        StatusMessage = "Preferences loaded from local storage.";
    }

    public async Task SaveAsync()
    {
        await _appPreferencesService.SaveAsync(BuildPreferences());

        StatusMessage = $"Preferences saved at {DateTime.Now:HH:mm:ss}.";
    }

    public async Task ToggleThemeAsync()
    {
        IsDarkMode = !IsDarkMode;
        await _appPreferencesService.SaveAsync(BuildPreferences());
        StatusMessage = $"Theme switched to {(IsDarkMode ? "dark" : "light")} mode at {DateTime.Now:HH:mm:ss}.";
    }

    private void ResetToDefaults()
    {
        AppTitle = "Project Template";
        PreferredAccentColor = "#0F766E";
        IsDarkMode = false;
        RememberWindowBounds = true;
        LastOpenedSection = "dashboard";
        StatusMessage = "Default values restored in the form.";
    }

    private AppPreferences BuildPreferences()
    {
        return new AppPreferences
        {
            AppTitle = AppTitle,
            PreferredAccentColor = PreferredAccentColor,
            IsDarkMode = IsDarkMode,
            RememberWindowBounds = RememberWindowBounds,
            LastOpenedSection = LastOpenedSection
        };
    }
}
