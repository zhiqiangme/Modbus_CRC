using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Project.ViewModels;
using Button = System.Windows.Controls.Button;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

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
                $"Application startup failed:{Environment.NewLine}{exception.Message}",
                "Project Template",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        ApplyCurrentTheme();
        MainContentTabControl.SelectedIndex = 0;
        UpdateTabButtonStyles();
    }

    private void PlaceholderTabButton1_OnClick(object sender, RoutedEventArgs e)
    {
        MainContentTabControl.SelectedIndex = 0;
    }

    private void PlaceholderTabButton2_OnClick(object sender, RoutedEventArgs e)
    {
        MainContentTabControl.SelectedIndex = 1;
    }

    private void MainContentTabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(sender, MainContentTabControl))
        {
            return;
        }

        UpdateTabButtonStyles();
    }

    private void UpdateTabButtonStyles()
    {
        ApplyTabButtonStyle(PlaceholderTabButton1, MainContentTabControl.SelectedIndex == 0);
        ApplyTabButtonStyle(PlaceholderTabButton2, MainContentTabControl.SelectedIndex == 1);
    }

    private static void ApplyTabButtonStyle(Button button, bool isActive)
    {
        if (isActive)
        {
            button.Background = (Brush)button.FindResource("TabButtonActiveBrush");
            button.BorderBrush = (Brush)button.FindResource("TabButtonActiveBorderBrush");
            button.Foreground = (Brush)button.FindResource("TabButtonActiveForegroundBrush");
            button.Effect = new DropShadowEffect
            {
                BlurRadius = 10,
                ShadowDepth = 1,
                Opacity = 0.16,
                Color = Color.FromRgb(15, 23, 42)
            };
        }
        else
        {
            button.Background = (Brush)button.FindResource("TitleBarBrush");
            button.BorderBrush = Brushes.Transparent;
            button.Foreground = (Brush)button.FindResource("TabButtonInactiveForegroundBrush");
            button.Effect = null;
        }
    }

    private void ViewModelOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainViewModel.IsDarkMode))
        {
            return;
        }

        ApplyCurrentTheme();
        UpdateTabButtonStyles();
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
