using System.Globalization;
using Project.Models;

namespace Project.Core;

public sealed class ModbusFrameService : IModbusFrameService
{
    public ModbusFrameResult BuildFrame(ModbusFrameInput input)
    {
        byte[] frameWithoutCrc = BuildRequestPayload(input);
        ushort crc = ComputeModbusCrc(frameWithoutCrc);
        byte[] frame = AppendCrc(frameWithoutCrc, crc);
        string rawFrame = FormatFrame(frame);
        // 剪贴板帧使用空格分隔，提高可读性。
        string clipboardFrame = string.Join(" ", frame.Select(static b => b.ToString("X2", CultureInfo.InvariantCulture)));

        return new ModbusFrameResult(input, crc, rawFrame, clipboardFrame, frame);
    }

    public FrameImportResult ImportFrame(string? frameText)
    {
        if (!TryReadHexBytes(frameText, out byte[] bytes, out string? errorMessage))
        {
            return FrameImportResult.Failure(errorMessage ?? "原始帧无效。");
        }

        if (bytes.Length < 6)
        {
            return FrameImportResult.Failure("原始帧至少需要 6 字节请求载荷。");
        }

        int payloadLength = GetRequestPayloadLength(bytes, out errorMessage);
        if (payloadLength <= 0)
        {
            return FrameImportResult.Failure(errorMessage ?? "不支持的请求帧。");
        }

        if (bytes.Length < payloadLength)
        {
            return FrameImportResult.Failure("原始帧长度不足，无法按功能码解析。");
        }

        byte[] payload = bytes[..payloadLength];
        bool isInputCrcValid = false;
        if (bytes.Length >= payloadLength + 2)
        {
            isInputCrcValid = IsFrameCrcValid(bytes[..(payloadLength + 2)]);
        }

        ModbusFrameInput input = CreateInputFromRequestPayload(payload);
        return FrameImportResult.Success(input, isInputCrcValid);
    }

    public ModbusResponseParseResult ParseResponse(string? frameText)
    {
        if (!TryReadHexBytes(frameText, out byte[] bytes, out string? errorMessage))
        {
            return ModbusResponseParseResult.Failure(errorMessage ?? "响应帧无效。");
        }

        if (bytes.Length < 5)
        {
            return ModbusResponseParseResult.Failure("响应帧至少需要 5 字节。");
        }

        bool isCrcValid = IsFrameCrcValid(bytes);
        byte[] payload = bytes[..^2];
        byte slaveAddress = payload[0];
        byte functionCode = payload[1];
        var details = new List<string>
        {
            $"从站地址：{slaveAddress} (0x{slaveAddress:X2})",
            $"功能码：0x{functionCode:X2}",
            $"CRC：{(isCrcValid ? "正确" : "错误")}"
        };

        if ((functionCode & 0x80) != 0)
        {
            if (payload.Length < 3)
            {
                details.Add("异常码：缺失");
                return ModbusResponseParseResult.Success(
                    isCrcValid,
                    FormatFrame(bytes),
                    "异常响应长度不足",
                    details);
            }

            byte exceptionCode = payload[2];
            string exceptionDescription = GetExceptionDescription(exceptionCode);
            details.Add($"异常码：0x{exceptionCode:X2}，{exceptionDescription}");
            return ModbusResponseParseResult.Success(
                isCrcValid,
                FormatFrame(bytes),
                $"异常响应：{exceptionDescription}",
                details);
        }

        string summary = functionCode switch
        {
            0x01 => ParseBitReadResponse(payload, "线圈", details),
            0x02 => ParseBitReadResponse(payload, "离散输入", details),
            0x03 => ParseRegisterReadResponse(payload, "保持寄存器", details),
            0x04 => ParseRegisterReadResponse(payload, "输入寄存器", details),
            0x05 => ParseSingleWriteEcho(payload, "写单线圈", details),
            0x06 => ParseSingleWriteEcho(payload, "写单寄存器", details),
            0x0F => ParseMultipleWriteEcho(payload, "写多线圈", details),
            0x10 => ParseMultipleWriteEcho(payload, "写多寄存器", details),
            _ => ParseUnknownResponse(payload, details)
        };

        return ModbusResponseParseResult.Success(isCrcValid, FormatFrame(bytes), summary, details);
    }

    public bool TryParseFieldValue(
        string? input,
        NumberBase numberBase,
        uint maxValue,
        out uint value,
        out string? errorMessage)
    {
        value = 0;

        if (string.IsNullOrWhiteSpace(input))
        {
            errorMessage = "请输入有效数值。";
            return false;
        }

        string cleaned = input.Trim().Replace(" ", string.Empty);
        if (cleaned.Length == 0)
        {
            errorMessage = "请输入有效数值。";
            return false;
        }

        if (numberBase == NumberBase.Hex)
        {
            if (cleaned.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned[2..];
            }

            if (cleaned.Length == 0)
            {
                errorMessage = "请输入十六进制数值。";
                return false;
            }

            if (cleaned.Any(static c => !Uri.IsHexDigit(c)))
            {
                errorMessage = "只能输入十六进制字符 0-9、A-F。";
                return false;
            }

            if (!uint.TryParse(cleaned, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value))
            {
                errorMessage = "十六进制数值无效或超出范围。";
                return false;
            }
        }
        else
        {
            if (cleaned.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned[2..];

                if (cleaned.Length == 0)
                {
                    errorMessage = "请输入十六进制数值。";
                    return false;
                }

                if (cleaned.Any(static c => !Uri.IsHexDigit(c)))
                {
                    errorMessage = "只能输入十六进制字符 0-9、A-F。";
                    return false;
                }

                if (!uint.TryParse(cleaned, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value))
                {
                    errorMessage = "十六进制数值无效或超出范围。";
                    return false;
                }
            }
            else
            {
                if (cleaned.StartsWith("DEC", StringComparison.OrdinalIgnoreCase))
                {
                    cleaned = cleaned[3..];
                }
                else if (cleaned.StartsWith("0d", StringComparison.OrdinalIgnoreCase))
                {
                    cleaned = cleaned[2..];
                }

                if (cleaned.Length == 0)
                {
                    errorMessage = "请输入十进制数值。";
                    return false;
                }

                if (cleaned.Any(static c => c is < '0' or > '9'))
                {
                    errorMessage = "十进制模式下只能输入 0-9。";
                    return false;
                }

                if (!uint.TryParse(cleaned, NumberStyles.None, CultureInfo.InvariantCulture, out value))
                {
                    errorMessage = "十进制数值无效或超出范围。";
                    return false;
                }
            }
        }

        if (value > maxValue)
        {
            errorMessage = $"数值超出范围，最大允许 {maxValue}。";
            return false;
        }

        errorMessage = null;
        return true;
    }

    public string FormatFieldValue(FrameFieldKey fieldKey, uint value, NumberBase numberBase)
    {
        int hexDigits = GetFieldHexDigits(fieldKey);
        return numberBase == NumberBase.Hex
            ? value.ToString($"X{hexDigits}", CultureInfo.InvariantCulture)
            : value.ToString(CultureInfo.InvariantCulture);
    }

    public uint GetFieldMaxValue(FrameFieldKey fieldKey)
    {
        return fieldKey switch
        {
            FrameFieldKey.SlaveAddress => byte.MaxValue,
            FrameFieldKey.FunctionCode => byte.MaxValue,
            FrameFieldKey.RegisterAddress => ushort.MaxValue,
            FrameFieldKey.Quantity => ushort.MaxValue,
            FrameFieldKey.Data => ushort.MaxValue,
            _ => ushort.MaxValue
        };
    }

    public int GetFieldHexDigits(FrameFieldKey fieldKey)
    {
        return fieldKey switch
        {
            FrameFieldKey.SlaveAddress => 2,
            FrameFieldKey.FunctionCode => 2,
            FrameFieldKey.RegisterAddress => 4,
            FrameFieldKey.Quantity => 4,
            FrameFieldKey.Data => 4,
            _ => 4
        };
    }

    private static byte[] BuildRequestPayload(ModbusFrameInput input)
    {
        return input.FunctionCode switch
        {
            0x01 or 0x02 or 0x03 or 0x04 => BuildAddressQuantityPayload(input),
            0x05 => BuildAddressValuePayload(input with { DataValue = NormalizeSingleCoilValue(input.DataValue) }),
            0x06 => BuildAddressValuePayload(input),
            0x0F => BuildWriteMultipleCoilsPayload(input),
            0x10 => BuildWriteMultipleRegistersPayload(input),
            _ => BuildAddressValuePayload(input)
        };
    }

    private static byte[] BuildAddressQuantityPayload(ModbusFrameInput input)
    {
        return
        [
            input.SlaveAddress,
            input.FunctionCode,
            (byte)(input.RegisterAddress >> 8),
            (byte)(input.RegisterAddress & 0xFF),
            (byte)(input.Quantity >> 8),
            (byte)(input.Quantity & 0xFF)
        ];
    }

    private static byte[] BuildAddressValuePayload(ModbusFrameInput input)
    {
        return
        [
            input.SlaveAddress,
            input.FunctionCode,
            (byte)(input.RegisterAddress >> 8),
            (byte)(input.RegisterAddress & 0xFF),
            (byte)(input.DataValue >> 8),
            (byte)(input.DataValue & 0xFF)
        ];
    }

    private static byte[] BuildWriteMultipleCoilsPayload(ModbusFrameInput input)
    {
        ushort[] values = (input.Values ?? Array.Empty<ushort>()).ToArray();
        ushort quantity = input.Quantity > 0 ? input.Quantity : (ushort)values.Length;
        int byteCount = (quantity + 7) / 8;
        var payload = new byte[7 + byteCount];
        payload[0] = input.SlaveAddress;
        payload[1] = input.FunctionCode;
        payload[2] = (byte)(input.RegisterAddress >> 8);
        payload[3] = (byte)(input.RegisterAddress & 0xFF);
        payload[4] = (byte)(quantity >> 8);
        payload[5] = (byte)(quantity & 0xFF);
        payload[6] = (byte)byteCount;

        // Modbus 多线圈写入按每字节低位到高位打包线圈状态。
        for (int i = 0; i < quantity && i < values.Length; i++)
        {
            if (values[i] != 0)
            {
                payload[7 + i / 8] |= (byte)(1 << (i % 8));
            }
        }

        return payload;
    }

    private static byte[] BuildWriteMultipleRegistersPayload(ModbusFrameInput input)
    {
        ushort[] values = (input.Values ?? Array.Empty<ushort>()).ToArray();
        ushort quantity = input.Quantity > 0 ? input.Quantity : (ushort)values.Length;
        var payload = new byte[7 + quantity * 2];
        payload[0] = input.SlaveAddress;
        payload[1] = input.FunctionCode;
        payload[2] = (byte)(input.RegisterAddress >> 8);
        payload[3] = (byte)(input.RegisterAddress & 0xFF);
        payload[4] = (byte)(quantity >> 8);
        payload[5] = (byte)(quantity & 0xFF);
        payload[6] = (byte)(quantity * 2);

        for (int i = 0; i < quantity && i < values.Length; i++)
        {
            int offset = 7 + i * 2;
            payload[offset] = (byte)(values[i] >> 8);
            payload[offset + 1] = (byte)(values[i] & 0xFF);
        }

        return payload;
    }

    private static int GetRequestPayloadLength(byte[] bytes, out string? errorMessage)
    {
        errorMessage = null;
        byte functionCode = bytes[1];
        if (functionCode is 0x01 or 0x02 or 0x03 or 0x04 or 0x05 or 0x06)
        {
            return 6;
        }

        if (functionCode is not 0x0F and not 0x10)
        {
            errorMessage = $"暂不支持导入功能码 0x{functionCode:X2} 的请求帧。";
            return 0;
        }

        if (bytes.Length < 7)
        {
            errorMessage = "多写请求帧缺少字节数字段。";
            return 0;
        }

        ushort quantity = ReadUInt16(bytes, 4);
        byte byteCount = bytes[6];
        int expectedByteCount = functionCode == 0x10 ? quantity * 2 : (quantity + 7) / 8;
        if (quantity == 0 || byteCount != expectedByteCount)
        {
            errorMessage = "多写请求帧的数量和字节数不匹配。";
            return 0;
        }

        return 7 + byteCount;
    }

    private static ModbusFrameInput CreateInputFromRequestPayload(byte[] payload)
    {
        byte functionCode = payload[1];
        ushort registerAddress = ReadUInt16(payload, 2);
        ushort dataValue = payload.Length >= 6 ? ReadUInt16(payload, 4) : (ushort)0;

        return functionCode switch
        {
            0x01 or 0x02 or 0x03 or 0x04 => new ModbusFrameInput(
                payload[0],
                functionCode,
                registerAddress,
                0,
                dataValue),
            0x0F => CreateMultipleCoilInput(payload),
            0x10 => CreateMultipleRegisterInput(payload),
            _ => new ModbusFrameInput(payload[0], functionCode, registerAddress, dataValue)
        };
    }

    private static ModbusFrameInput CreateMultipleCoilInput(byte[] payload)
    {
        ushort quantity = ReadUInt16(payload, 4);
        ushort[] values = new ushort[quantity];
        for (int i = 0; i < quantity; i++)
        {
            values[i] = (payload[7 + i / 8] & (1 << (i % 8))) != 0 ? (ushort)1 : (ushort)0;
        }

        return new ModbusFrameInput(payload[0], payload[1], ReadUInt16(payload, 2), 0, quantity, values);
    }

    private static ModbusFrameInput CreateMultipleRegisterInput(byte[] payload)
    {
        ushort quantity = ReadUInt16(payload, 4);
        ushort[] values = new ushort[quantity];
        for (int i = 0; i < quantity; i++)
        {
            values[i] = ReadUInt16(payload, 7 + i * 2);
        }

        return new ModbusFrameInput(payload[0], payload[1], ReadUInt16(payload, 2), 0, quantity, values);
    }

    private static string ParseBitReadResponse(byte[] payload, string label, List<string> details)
    {
        if (payload.Length < 3)
        {
            details.Add("数据区：缺失字节数。");
            return $"{label}读取响应长度不足";
        }

        byte byteCount = payload[2];
        if (payload.Length < 3 + byteCount)
        {
            details.Add("数据区：实际长度小于字节数。");
            return $"{label}读取响应长度不足";
        }

        details.Add($"数据字节数：{byteCount}");
        int bitCount = byteCount * 8;
        for (int i = 0; i < bitCount; i++)
        {
            bool bitSet = (payload[3 + i / 8] & (1 << (i % 8))) != 0;
            details.Add($"{label} {i}：{(bitSet ? "ON" : "OFF")}");
        }

        return $"{label}读取响应，{byteCount} 字节数据";
    }

    private static string ParseRegisterReadResponse(byte[] payload, string label, List<string> details)
    {
        if (payload.Length < 3)
        {
            details.Add("数据区：缺失字节数。");
            return $"{label}读取响应长度不足";
        }

        byte byteCount = payload[2];
        if (payload.Length < 3 + byteCount)
        {
            details.Add("数据区：实际长度小于字节数。");
            return $"{label}读取响应长度不足";
        }

        if (byteCount % 2 != 0)
        {
            details.Add("数据区：寄存器字节数不是偶数。");
        }

        int registerCount = byteCount / 2;
        details.Add($"寄存器数量：{registerCount}");
        for (int i = 0; i < registerCount; i++)
        {
            ushort value = ReadUInt16(payload, 3 + i * 2);
            details.Add($"{label} {i}：{value} (0x{value:X4})");
        }

        return $"{label}读取响应，{registerCount} 个寄存器";
    }

    private static string ParseSingleWriteEcho(byte[] payload, string action, List<string> details)
    {
        if (payload.Length < 6)
        {
            details.Add("回显数据：长度不足。");
            return $"{action}响应长度不足";
        }

        ushort address = ReadUInt16(payload, 2);
        ushort value = ReadUInt16(payload, 4);
        details.Add($"地址：{address} (0x{address:X4})");
        details.Add($"值：{value} (0x{value:X4})");
        return $"{action}成功回显";
    }

    private static string ParseMultipleWriteEcho(byte[] payload, string action, List<string> details)
    {
        if (payload.Length < 6)
        {
            details.Add("回显数据：长度不足。");
            return $"{action}响应长度不足";
        }

        ushort address = ReadUInt16(payload, 2);
        ushort quantity = ReadUInt16(payload, 4);
        details.Add($"起始地址：{address} (0x{address:X4})");
        details.Add($"数量：{quantity}");
        return $"{action}成功回显";
    }

    private static string ParseUnknownResponse(byte[] payload, List<string> details)
    {
        string data = payload.Length > 2 ? FormatFrame(payload[2..]) : "无";
        details.Add($"数据区：{data}");
        return "未知功能码响应";
    }

    private static string CleanFrameText(string? input)
    {
        string cleaned = new((input ?? string.Empty)
            .Where(static c => !char.IsWhiteSpace(c) && c is not '-' and not ',' and not ':')
            .ToArray());
        return cleaned.Replace("0x", string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryReadHexBytes(string? input, out byte[] bytes, out string? errorMessage)
    {
        bytes = [];
        string cleaned = CleanFrameText(input);
        if (cleaned.Length == 0)
        {
            errorMessage = "原始帧为空。";
            return false;
        }

        if (cleaned.Length % 2 != 0)
        {
            errorMessage = "十六进制字符数量必须为偶数。";
            return false;
        }

        if (cleaned.Any(static c => !Uri.IsHexDigit(c)))
        {
            errorMessage = "原始帧包含非十六进制字符。";
            return false;
        }

        bytes = Enumerable.Range(0, cleaned.Length / 2)
            .Select(i => Convert.ToByte(cleaned.Substring(i * 2, 2), 16))
            .ToArray();
        errorMessage = null;
        return true;
    }

    private static byte[] AppendCrc(byte[] payload, ushort crc)
    {
        var frame = new byte[payload.Length + 2];
        Array.Copy(payload, frame, payload.Length);
        // Modbus RTU 的 CRC 帧尾顺序是低字节在前。
        frame[^2] = (byte)(crc & 0xFF);
        frame[^1] = (byte)(crc >> 8);
        return frame;
    }

    private static bool IsFrameCrcValid(byte[] frame)
    {
        if (frame.Length < 3)
        {
            return false;
        }

        ushort expectedCrc = ComputeModbusCrc(frame[..^2]);
        ushort inputCrc = (ushort)(frame[^2] | (frame[^1] << 8));
        return expectedCrc == inputCrc;
    }

    private static string FormatFrame(byte[] frame)
    {
        return string.Join(" ", frame.Select(static b => b.ToString("X2", CultureInfo.InvariantCulture)));
    }

    private static ushort NormalizeSingleCoilValue(ushort value)
    {
        return value == 0 ? (ushort)0x0000 : (ushort)0xFF00;
    }

    private static ushort ReadUInt16(byte[] bytes, int offset)
    {
        return (ushort)((bytes[offset] << 8) | bytes[offset + 1]);
    }

    private static string GetExceptionDescription(byte exceptionCode)
    {
        return exceptionCode switch
        {
            0x01 => "非法功能",
            0x02 => "非法数据地址",
            0x03 => "非法数据值",
            0x04 => "从站设备故障",
            0x05 => "确认，稍后处理",
            0x06 => "从站设备忙",
            0x08 => "存储奇偶性错误",
            0x0A => "网关路径不可用",
            0x0B => "网关目标设备无响应",
            _ => "未知异常"
        };
    }

    private static ushort ComputeModbusCrc(IEnumerable<byte> data)
    {
        ushort crc = 0xFFFF;

        foreach (byte value in data)
        {
            crc ^= value;

            for (int i = 0; i < 8; i++)
            {
                bool leastBitSet = (crc & 0x0001) != 0;
                crc >>= 1;

                if (leastBitSet)
                {
                    crc ^= 0xA001;
                }
            }
        }

        return crc;
    }
}
