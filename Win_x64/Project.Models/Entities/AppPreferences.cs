namespace Project.Models;

public sealed class AppPreferences
{
    public string AppTitle { get; set; } = "Modbus RTU 原始帧生成器";

    public string PreferredAccentColor { get; set; } = "#0F766E";

    public bool IsDarkMode { get; set; }

    public bool RememberWindowBounds { get; set; } = true;
}
