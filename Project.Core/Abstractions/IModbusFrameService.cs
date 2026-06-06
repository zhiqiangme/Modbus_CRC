using Project.Models;

namespace Project.Core;

public interface IModbusFrameService
{
    ModbusFrameResult BuildFrame(ModbusFrameInput input);

    FrameImportResult ImportFrame(string? frameText);

    bool TryParseFieldValue(
        string? input,
        NumberBase numberBase,
        uint maxValue,
        out uint value,
        out string? errorMessage);

    string FormatFieldValue(FrameFieldKey fieldKey, uint value, NumberBase numberBase);

    uint GetFieldMaxValue(FrameFieldKey fieldKey);

    int GetFieldHexDigits(FrameFieldKey fieldKey);
}
