namespace Project.Models;

public sealed record ModbusFrameInput(
    byte SlaveAddress,
    byte FunctionCode,
    ushort RegisterAddress,
    ushort DataValue);
