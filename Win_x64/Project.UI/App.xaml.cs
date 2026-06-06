using System.Collections.Generic;
using System.Windows.Media;
using Project.Core;
using Project.Infrastructure;
using Project.UI.Services;
using Project.ViewModels;
using Application = System.Windows.Application;
using StartupEventArgs = System.Windows.StartupEventArgs;

namespace Project.UI;

public partial class App : Application
{
    private static readonly IReadOnlyDictionary<string, string> LightThemeBrushes = new Dictionary<string, string>
    {
        ["WindowBackgroundBrush"] = "#F9F9FB",
        ["PrimaryBrush"] = "#0F766E",
        ["PrimaryHoverBrush"] = "#0D9488",
        ["PrimaryPressedBrush"] = "#115E59",
        ["SecondaryBrush"] = "#ECFDF5",
        ["TextPrimaryBrush"] = "#111827",
        ["TextSecondaryBrush"] = "#4B5563",
        ["BorderBrush"] = "#D1D5DB",
        ["PanelBrush"] = "#F9F9FB",
        ["SelectedNavigationBrush"] = "#D1FAE5",
        ["SurfaceBrush"] = "#F9FAFB",
        ["TitleBarBrush"] = "#EAEAED",
        ["TitleBarBorderBrush"] = "#D8DEE6",
        ["TitleBarButtonHoverBrush"] = "#E5E7EB",
        ["TitleBarIconBrush"] = "#475569",
        ["TitleBarIconHoverBrush"] = "#0F172A",
        ["CloseButtonHoverBrush"] = "#DC2626",
        ["CloseButtonIconHoverBrush"] = "#FFFFFF",
        ["TabButtonActiveBrush"] = "#FFFFFF",
        ["TabButtonActiveBorderBrush"] = "#D5DAE3",
        ["TabButtonActiveForegroundBrush"] = "#0F172A",
        ["TabButtonInactiveForegroundBrush"] = "#4B5563"
    };

    private static readonly IReadOnlyDictionary<string, string> DarkThemeBrushes = new Dictionary<string, string>
    {
        ["WindowBackgroundBrush"] = "#181818",
        ["PrimaryBrush"] = "#14B8A6",
        ["PrimaryHoverBrush"] = "#2DD4BF",
        ["PrimaryPressedBrush"] = "#0F766E",
        ["SecondaryBrush"] = "#11362F",
        ["TextPrimaryBrush"] = "#F9FAFB",
        ["TextSecondaryBrush"] = "#CBD5E1",
        ["BorderBrush"] = "#334155",
        ["PanelBrush"] = "#1E293B",
        ["SelectedNavigationBrush"] = "#134E4A",
        ["SurfaceBrush"] = "#0F172A",
        ["TitleBarBrush"] = "#181818",
        ["TitleBarBorderBrush"] = "#334155",
        ["TitleBarButtonHoverBrush"] = "#1F2937",
        ["TitleBarIconBrush"] = "#CBD5E1",
        ["TitleBarIconHoverBrush"] = "#F8FAFC",
        ["CloseButtonHoverBrush"] = "#DC2626",
        ["CloseButtonIconHoverBrush"] = "#FFFFFF",
        ["TabButtonActiveBrush"] = "#282828",
        ["TabButtonActiveBorderBrush"] = "#475569",
        ["TabButtonActiveForegroundBrush"] = "#F8FAFC",
        ["TabButtonInactiveForegroundBrush"] = "#CBD5E1"
    };

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        IAppPreferencesService appPreferencesService = new AppPreferencesService();
        IModbusFrameService modbusFrameService = new ModbusFrameService();
        IClipboardService clipboardService = new WpfClipboardService();
        IModbusSerialService modbusSerialService = new ModbusSerialService();

        var modbusCrcViewModel = new ModbusCrcViewModel(modbusFrameService, clipboardService, modbusSerialService);
        var mainViewModel = new MainViewModel(appPreferencesService, modbusCrcViewModel);

        var mainWindow = new MainWindow(mainViewModel);
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    public void ApplyTheme(bool isDarkMode)
    {
        var palette = isDarkMode ? DarkThemeBrushes : LightThemeBrushes;

        foreach (var (resourceKey, colorValue) in palette)
        {
            Resources[resourceKey] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorValue)!);
        }
    }
}
