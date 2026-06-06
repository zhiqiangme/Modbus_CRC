namespace Project.Models;

public sealed record FrameImportResult(
    bool IsSuccess,
    ModbusFrameInput? Input,
    bool IsInputCrcValid,
    string? ErrorMessage)
{
    public bool IsCorrected => IsSuccess && !IsInputCrcValid;

    public static FrameImportResult Success(ModbusFrameInput input, bool isInputCrcValid)
    {
        return new FrameImportResult(true, input, isInputCrcValid, null);
    }

    public static FrameImportResult Failure(string errorMessage)
    {
        return new FrameImportResult(false, null, false, errorMessage);
    }
}
