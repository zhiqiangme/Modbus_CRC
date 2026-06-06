using System.Windows;
using Project.Core;

namespace Project.UI.Services;

public sealed class WpfClipboardService : IClipboardService
{
    public bool ContainsText()
    {
        return Clipboard.ContainsText();
    }

    public string GetText()
    {
        return Clipboard.GetText();
    }

    public void SetText(string text)
    {
        Clipboard.SetText(text);
    }
}
