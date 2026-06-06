using System.Globalization;
using Project.Models;

namespace Project.Core;

public sealed class ModbusFrameService : IModbusFrameService
{
    public ModbusFrameResult BuildFrame(ModbusFrameInput input)
    {
        byte[] frameWithoutCrc =
        [
            input.SlaveAddress,
            input.FunctionCode,
            (byte)(input.RegisterAddress >> 8),
            (byte)(input.RegisterAddress & 0xFF),
            (byte)(input.DataValue >> 8),
            (byte)(input.DataValue & 0xFF)
        ];

        ushort crc = ComputeModbusCrc(frameWithoutCrc);
        byte[] frame =
        [
            frameWithoutCrc[0],
            frameWithoutCrc[1],
            frameWithoutCrc[2],
            frameWithoutCrc[3],
            frameWithoutCrc[4],
            frameWithoutCrc[5],
            // Modbus RTU 的 CRC 帧尾顺序是低字节在前。
            (byte)(crc & 0xFF),
            (byte)(crc >> 8)
        ];

        string rawFrame = string.Join(" ", frame.Select(static b => b.ToString("X2")));
        string clipboardFrame = string.Concat(frame.Select(static b => b.ToString("X2")));

        return new ModbusFrameResult(input, crc, rawFrame, clipboardFrame);
    }

    public FrameImportResult ImportFrame(string? frameText)
    {
        string cleaned = CleanFrameText(frameText);
        if (cleaned.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[2..];
        }

        if (cleaned.Length == 0)
        {
            return FrameImportResult.Failure("原始帧为空。");
        }

        if (cleaned.Length > 16)
        {
            cleaned = cleaned[..16];
        }

        if (cleaned.Length < 12)
        {
            return FrameImportResult.Failure("原始帧至少需要前 12 位有效十六进制字符。");
        }

        string payloadHex = cleaned[..12];
        if (payloadHex.Any(static c => !Uri.IsHexDigit(c)))
        {
            return FrameImportResult.Failure("原始帧前 12 位包含非十六进制字符。");
        }

        string trailingCrc = cleaned.Length > 12 ? cleaned[12..] : string.Empty;
        if (trailingCrc.Length > 0 && trailingCrc.Any(static c => !Uri.IsHexDigit(c)))
        {
            return FrameImportResult.Failure("原始帧 CRC 部分包含非十六进制字符。");
        }

        byte[] bytes = Enumerable.Range(0, payloadHex.Length / 2)
            .Select(i => Convert.ToByte(payloadHex.Substring(i * 2, 2), 16))
            .ToArray();

        var input = new ModbusFrameInput(
            bytes[0],
            bytes[1],
            (ushort)((bytes[2] << 8) | bytes[3]),
            (ushort)((bytes[4] << 8) | bytes[5]));

        bool isInputCrcValid = false;
        if (trailingCrc.Length == 4)
        {
            ushort expectedCrc = ComputeModbusCrc(bytes);
            ushort inputCrc = (ushort)(
                Convert.ToByte(trailingCrc[..2], 16)
                | (Convert.ToByte(trailingCrc.Substring(2, 2), 16) << 8));
            isInputCrcValid = expectedCrc == inputCrc;
        }

        return FrameImportResult.Success(input, isInputCrcValid);
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
            FrameFieldKey.Data => 4,
            _ => 4
        };
    }

    private static string CleanFrameText(string? input)
    {
        return new string((input ?? string.Empty)
            .Where(static c => !char.IsWhiteSpace(c) && c is not '-' and not ',')
            .ToArray());
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
