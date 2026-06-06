using System.Diagnostics;
using Project.Core;
using Project.Models;
using RJCP.IO.Ports;

namespace Project.Infrastructure;

public sealed class ModbusSerialService : IModbusSerialService
{
    public IReadOnlyList<string> GetPortNames()
    {
        using var serialPort = new SerialPortStream();
        return serialPort.GetPortNames()
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public Task<ModbusSerialExchangeResult> SendAndReceiveAsync(
        byte[] requestBytes,
        ModbusSerialSettings settings,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => SendAndReceive(requestBytes, settings, cancellationToken), cancellationToken);
    }

    private static ModbusSerialExchangeResult SendAndReceive(
        byte[] requestBytes,
        ModbusSerialSettings settings,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var serialPort = new SerialPortStream
            {
                PortName = settings.PortName,
                BaudRate = settings.BaudRate,
                DataBits = settings.DataBits,
                Parity = ParseParity(settings.Parity),
                StopBits = ParseStopBits(settings.StopBits),
                ReadTimeout = 100,
                WriteTimeout = Math.Max(100, settings.TimeoutMilliseconds)
            };

            serialPort.Open();
            serialPort.DiscardInBuffer();
            serialPort.DiscardOutBuffer();
            serialPort.Write(requestBytes, 0, requestBytes.Length);
            serialPort.Flush();

            byte[] buffer = new byte[256];
            var response = new List<byte>();
            int timeout = Math.Max(100, settings.TimeoutMilliseconds);

            // 收到首批响应后，再等待一个短读周期，避免过早截断慢速串口帧。
            while (stopwatch.ElapsedMilliseconds < timeout)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    int read = serialPort.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                    {
                        continue;
                    }

                    response.AddRange(buffer.Take(read));
                }
                catch (TimeoutException)
                {
                    if (response.Count > 0)
                    {
                        break;
                    }
                }
            }

            stopwatch.Stop();
            return response.Count == 0
                ? new ModbusSerialExchangeResult(false, [], "发送完成，但未收到响应。", stopwatch.Elapsed)
                : new ModbusSerialExchangeResult(true, response.ToArray(), "发送成功，已收到响应。", stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            return new ModbusSerialExchangeResult(false, [], "发送已取消。", stopwatch.Elapsed);
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            return new ModbusSerialExchangeResult(false, [], $"串口发送失败：{exception.Message}", stopwatch.Elapsed);
        }
    }

    private static Parity ParseParity(string value)
    {
        return value switch
        {
            "Odd" => Parity.Odd,
            "Even" => Parity.Even,
            _ => Parity.None
        };
    }

    private static StopBits ParseStopBits(string value)
    {
        return value switch
        {
            "Two" => StopBits.Two,
            _ => StopBits.One
        };
    }
}
