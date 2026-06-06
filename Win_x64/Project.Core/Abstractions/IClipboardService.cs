namespace Project.Core;

public interface IClipboardService
{
    bool ContainsText();

    string GetText();

    void SetText(string text);
}
