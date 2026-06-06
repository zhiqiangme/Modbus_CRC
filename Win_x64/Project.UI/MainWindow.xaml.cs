using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Project.ViewModels;

namespace Project.UI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;

        InitializeComponent();
        Loaded += MainWindow_Loaded;
        SourceInitialized += MainWindow_SourceInitialized;
        _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
        Closed += MainWindow_Closed;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= MainWindow_Loaded;
        try
        {
            await _viewModel.InitializeAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"应用启动失败：{Environment.NewLine}{exception.Message}",
                "Modbus CRC",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        ApplyCurrentTheme();
    }

    private void ViewModelOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainViewModel.IsDarkMode))
        {
            return;
        }

        ApplyCurrentTheme();
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        SourceInitialized -= MainWindow_SourceInitialized;
        _viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        Closed -= MainWindow_Closed;
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        ApplySystemRoundedCorners();
    }

    private void ApplyCurrentTheme()
    {
        if (Application.Current is App app)
        {
            app.ApplyTheme(_viewModel.IsDarkMode);
        }
    }

    private void ApplySystemRoundedCorners()
    {
        var windowHandle = new WindowInteropHelper(this).Handle;
        if (windowHandle == IntPtr.Zero)
        {
            return;
        }

        try
        {
            var cornerPreference = (int)DwmWindowCornerPreference.Round;
            _ = DwmSetWindowAttribute(
                windowHandle,
                DwmWindowAttribute.WindowCornerPreference,
                ref cornerPreference,
                sizeof(int));
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 1)
        {
            return;
        }

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        DwmWindowAttribute attribute,
        ref int value,
        int valueSize);

    private enum DwmWindowAttribute
    {
        WindowCornerPreference = 33
    }

    private enum DwmWindowCornerPreference
    {
        Default = 0,
        DoNotRound = 1,
        Round = 2,
        RoundSmall = 3
    }
}
