using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Project.ViewModels;

namespace Project.UI.Views;

public partial class ModbusCrcView : UserControl
{
    public ModbusCrcView()
    {
        InitializeComponent();
    }

    private void FrameImportTextBox_OnPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (ShouldOpenNativeContextMenu())
        {
            return;
        }

        e.Handled = true;
        ExecuteViewModelCommand(static viewModel => viewModel.ImportFromClipboardCommand);
    }

    private void ResponseImportTextBox_OnPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (ShouldOpenNativeContextMenu())
        {
            return;
        }

        e.Handled = true;
        ExecuteViewModelCommand(static viewModel => viewModel.ImportResponseFromClipboardCommand);
    }

    private void FrameImportTextBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        e.Handled = true;
        ExecuteViewModelCommand(static viewModel => viewModel.ImportFrameTextCommand);
    }

    private void RecycleBinButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ModbusCrcViewModel viewModel)
        {
            return;
        }

        Window recycleWindow = new()
        {
            Title = "回收站",
            Owner = Window.GetWindow(this),
            Width = 720,
            Height = 480,
            MinWidth = 560,
            MinHeight = 360,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Brushes.White
        };

        TextBox contentBox = new()
        {
            Margin = new Thickness(16),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 15,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            Text = viewModel.RecycleBin.Count == 0
                ? "回收站为空。"
                : string.Join(
                    Environment.NewLine + Environment.NewLine,
                    viewModel.RecycleBin.Select((item, index) => $"{index + 1}. {item}"))
        };

        recycleWindow.Content = contentBox;
        recycleWindow.ShowDialog();
    }

    private void ExecuteViewModelCommand(Func<ModbusCrcViewModel, ICommand> commandSelector)
    {
        if (DataContext is not ModbusCrcViewModel viewModel)
        {
            return;
        }

        ICommand command = commandSelector(viewModel);
        if (command.CanExecute(null))
        {
            // UI 事件只转发命令，业务逻辑保留在 ViewModel。
            command.Execute(null);
        }
    }

    private static bool ShouldOpenNativeContextMenu()
    {
        const ModifierKeys supportedModifiers = ModifierKeys.Control
            | ModifierKeys.Shift
            | ModifierKeys.Alt
            | ModifierKeys.Windows;

        // Fn 键通常不会被 Windows/WPF 当作普通修饰键上报。
        return (Keyboard.Modifiers & supportedModifiers) != ModifierKeys.None;
    }
}
