namespace Project.Models;

public sealed record ModbusFrameInput(
    byte SlaveAddress,
    byte FunctionCode,
    ushort RegisterAddress,
    ushort DataValue,
    ushort Quantity = 1,
    IReadOnlyList<ushort>? Values = null);
