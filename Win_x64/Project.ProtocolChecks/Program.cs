using Project.Core;
using Project.Models;

var service = new ModbusFrameService();

// 覆盖无硬件条件下最关键的协议层输入输出。
CheckRequestFrames(service);
CheckImportCorrection(service);
CheckResponseParsing(service);

Console.WriteLine("协议层验证通过。");

static void CheckRequestFrames(IModbusFrameService service)
{
    // 期望帧来自独立 CRC 计算，避免验证只重复生产实现。
    AssertFrame(
        service.BuildFrame(new ModbusFrameInput(0x01, 0x01, 0x0013, 0, 0x0025)),
        "01 01 00 13 00 25 0C 14",
        "功能码 01 读线圈");

    AssertFrame(
        service.BuildFrame(new ModbusFrameInput(0x01, 0x02, 0x0013, 0, 0x0025)),
        "01 02 00 13 00 25 48 14",
        "功能码 02 读离散输入");

    AssertFrame(
        service.BuildFrame(new ModbusFrameInput(0x01, 0x03, 0x0000, 0, 0x000A)),
        "01 03 00 00 00 0A C5 CD",
        "功能码 03 读保持寄存器");

    AssertFrame(
        service.BuildFrame(new ModbusFrameInput(0x01, 0x04, 0x0000, 0, 0x000A)),
        "01 04 00 00 00 0A 70 0D",
        "功能码 04 读输入寄存器");

    AssertFrame(
        service.BuildFrame(new ModbusFrameInput(0x01, 0x05, 0x0013, 1)),
        "01 05 00 13 FF 00 7D FF",
        "功能码 05 写单线圈");

    AssertFrame(
        service.BuildFrame(new ModbusFrameInput(0x01, 0x06, 0x0001, 0x0003)),
        "01 06 00 01 00 03 98 0B",
        "功能码 06 写单寄存器");

    AssertFrame(
        service.BuildFrame(new ModbusFrameInput(
            0x11,
            0x0F,
            0x0013,
            0,
            0x000A,
            [1, 0, 1, 1, 0, 0, 1, 1, 1, 0])),
        "11 0F 00 13 00 0A 02 CD 01 BF 0B",
        "功能码 0F 写多线圈");

    AssertFrame(
        service.BuildFrame(new ModbusFrameInput(
            0x11,
            0x10,
            0x0001,
            0,
            0x0002,
            [0x000A, 0x0102])),
        "11 10 00 01 00 02 04 00 0A 01 02 C6 F0",
        "功能码 10 写多寄存器");
}

static void CheckImportCorrection(IModbusFrameService service)
{
    // 导入错误 CRC 的请求帧时，应保留载荷并重新生成正确帧。
    FrameImportResult importResult = service.ImportFrame("0x01,03,00,00,00,0A,00,00");
    Assert(importResult.IsSuccess, "请求帧导入应成功。");
    Assert(importResult.IsCorrected, "错误 CRC 请求帧应标记为已纠正。");
    Assert(importResult.Input is not null, "导入结果应包含请求输入。");

    ModbusFrameResult correctedFrame = service.BuildFrame(importResult.Input!);
    AssertFrame(correctedFrame, "01 03 00 00 00 0A C5 CD", "导入纠错后的请求帧");
}

static void CheckResponseParsing(IModbusFrameService service)
{
    // 响应解析同时验证正常响应、异常响应和 CRC 错误响应。
    ModbusResponseParseResult readResponse = service.ParseResponse("01 03 02 00 0A 38 43");
    Assert(readResponse.IsSuccess, "保持寄存器响应解析应成功。");
    Assert(readResponse.IsCrcValid, "保持寄存器响应 CRC 应正确。");
    Assert(readResponse.Summary.Contains("1 个寄存器", StringComparison.Ordinal), "响应摘要应显示寄存器数量。");
    Assert(
        readResponse.Details.Any(item => item.Contains("保持寄存器 0：10 (0x000A)", StringComparison.Ordinal)),
        "响应详情应显示寄存器原始值。");

    ModbusResponseParseResult exceptionResponse = service.ParseResponse("01 83 02 C0 F1");
    Assert(exceptionResponse.IsSuccess, "异常响应解析应成功。");
    Assert(exceptionResponse.IsCrcValid, "异常响应 CRC 应正确。");
    Assert(exceptionResponse.Summary.Contains("非法数据地址", StringComparison.Ordinal), "异常响应应显示中文异常说明。");

    ModbusResponseParseResult badCrcResponse = service.ParseResponse("01 03 02 00 0A 00 00");
    Assert(badCrcResponse.IsSuccess, "CRC 错误但格式有效的响应仍应解析。");
    Assert(!badCrcResponse.IsCrcValid, "CRC 错误响应应标记为 CRC 错误。");
}

static void AssertFrame(ModbusFrameResult result, string expectedFrame, string scenario)
{
    Assert(
        string.Equals(result.RawFrameDisplay, expectedFrame, StringComparison.Ordinal),
        $"{scenario} 帧不匹配：期望 {expectedFrame}，实际 {result.RawFrameDisplay}");
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
