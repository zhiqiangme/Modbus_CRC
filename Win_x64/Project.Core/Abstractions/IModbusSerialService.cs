using Project.Models;

namespace Project.Core;

public interface IModbusSerialService
{
    IReadOnlyList<string> GetPortNames();

    Task<ModbusSerialExchangeResult> SendAndReceiveAsync(
        byte[] requestBytes,
        ModbusSerialSettings settings,
        CancellationToken cancellationToken = default);
}
