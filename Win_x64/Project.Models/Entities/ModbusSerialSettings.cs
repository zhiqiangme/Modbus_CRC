namespace Project.Models;

public sealed record ModbusSerialSettings(
    string PortName,
    int BaudRate,
    int DataBits,
    string Parity,
    string StopBits,
    int TimeoutMilliseconds);
