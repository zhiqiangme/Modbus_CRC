namespace Project.Models;

public sealed record ModbusFrameResult(
    ModbusFrameInput Input,
    ushort Crc,
    string RawFrameDisplay,
    string ClipboardFrame);
