namespace Project.Models;

public sealed record ModbusResponseParseResult(
    bool IsSuccess,
    bool IsCrcValid,
    string RawFrameDisplay,
    string Summary,
    IReadOnlyList<string> Details,
    string? ErrorMessage)
{
    public static ModbusResponseParseResult Success(
        bool isCrcValid,
        string rawFrameDisplay,
        string summary,
        IReadOnlyList<string> details)
    {
        return new ModbusResponseParseResult(true, isCrcValid, rawFrameDisplay, summary, details, null);
    }

    public static ModbusResponseParseResult Failure(string errorMessage)
    {
        return new ModbusResponseParseResult(false, false, string.Empty, "解析失败", [], errorMessage);
    }
}
