namespace Project.Models;

public sealed record ModbusSerialExchangeResult(
    bool IsSuccess,
    byte[] ResponseBytes,
    string StatusText,
    TimeSpan Elapsed);
