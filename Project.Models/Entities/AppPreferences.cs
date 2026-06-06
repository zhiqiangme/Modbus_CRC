namespace Project.Models;

public sealed class AppPreferences
{
    public string AppTitle { get; set; } = "Project Template";

    public string PreferredAccentColor { get; set; } = "#0F766E";

    public bool IsDarkMode { get; set; }

    public bool RememberWindowBounds { get; set; } = true;

    public string LastOpenedSection { get; set; } = "dashboard";
}
