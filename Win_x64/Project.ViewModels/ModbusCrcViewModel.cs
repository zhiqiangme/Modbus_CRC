using System.Collections.ObjectModel;
using System.Globalization;
using Project.Core;
using Project.Models;

namespace Project.ViewModels;

public sealed class ModbusCrcViewModel : ObservableObject
{
    private readonly IModbusFrameService _modbusFrameService;
    private readonly IClipboardService _clipboardService;
    private readonly IModbusSerialService _modbusSerialService;

    private bool _suppressAutoGenerate;
    private byte[] _currentFrameBytes = [];
    private string _slaveAddressText = string.Empty;
    private string _functionCodeText = string.Empty;
    private string _registerAddressText = string.Empty;
    private string _quantityText = string.Empty;
    private string _dataText = string.Empty;
    private string _valuesText = string.Empty;
    private string _frameImportText = string.Empty;
    private string _frameImportStatusText = string.Empty;
    private bool _isFrameImportStatusVisible;
    private bool _isFrameImportStatusWarning;
    private NumberBase _slaveAddressBase = NumberBase.Dec;
    private NumberBase _registerAddressBase = NumberBase.Hex;
    private NumberBase _quantityBase = NumberBase.Dec;
    private NumberBase _dataBase = NumberBase.Dec;
    private ModbusFunctionOption _selectedFunctionOption;
    private string _headerStatusText = "等待输入";
    private string _rawFrameText = string.Empty;
    private string _crcText = "--";
    private string _copyStatusText = "等待生成";
    private string _nextStepText = "先输入完整参数，然后点击“生成并复制”。";
    private string _responseImportText = string.Empty;
    private string _responseSummaryText = "等待响应帧";
    private string _responseDetailsText = string.Empty;
    private string _responseCrcStatusText = "--";
    private string _serialStatusText = "未连接";
    private string _selectedSerialPortName = string.Empty;
    private string _baudRateText = "9600";
    private string _dataBitsText = "8";
    private string _selectedParityOption = "None";
    private string _selectedStopBitsOption = "One";
    private string _serialTimeoutText = "1000";

    public ModbusCrcViewModel(
        IModbusFrameService modbusFrameService,
        IClipboardService clipboardService,
        IModbusSerialService modbusSerialService)
    {
        _modbusFrameService = modbusFrameService;
        _clipboardService = clipboardService;
        _modbusSerialService = modbusSerialService;

        FunctionOptions =
        [
            new(0x01, "读线圈", RequestDataMode.ReadBits),
            new(0x02, "读离散输入", RequestDataMode.ReadBits),
            new(0x03, "读保持寄存器", RequestDataMode.ReadRegisters),
            new(0x04, "读输入寄存器", RequestDataMode.ReadRegisters),
            new(0x05, "写单线圈", RequestDataMode.WriteSingleCoil),
            new(0x06, "写单寄存器", RequestDataMode.WriteSingleRegister),
            new(0x0F, "写多线圈", RequestDataMode.WriteMultipleCoils),
            new(0x10, "写多寄存器", RequestDataMode.WriteMultipleRegisters)
        ];
        _selectedFunctionOption = FunctionOptions.First(static option => option.Code == 0x06);

        GenerateCommand = new RelayCommand(() => GenerateAndCopyFrame(addHistory: true));
        FillExampleCommand = new RelayCommand(FillExample);
        ImportFromClipboardCommand = new RelayCommand(ImportFrameFromClipboard);
        ImportFrameTextCommand = new RelayCommand(ImportFrameFromTextBox);
        ParseResponseCommand = new RelayCommand(ParseResponseFromTextBox);
        RefreshSerialPortsCommand = new RelayCommand(RefreshSerialPorts);
        SendCurrentFrameCommand = new AsyncRelayCommand(SendCurrentFrameAsync);
        ToggleSlaveAddressBaseCommand = new RelayCommand(() => ToggleFieldBase(FrameFieldKey.SlaveAddress));
        ToggleRegisterAddressBaseCommand = new RelayCommand(() => ToggleFieldBase(FrameFieldKey.RegisterAddress));
        ToggleQuantityBaseCommand = new RelayCommand(() => ToggleFieldBase(FrameFieldKey.Quantity));
        ToggleDataBaseCommand = new RelayCommand(() => ToggleFieldBase(FrameFieldKey.Data));

        FillExample();
        RefreshSerialPorts();
    }

    public RelayCommand GenerateCommand { get; }

    public RelayCommand FillExampleCommand { get; }

    public RelayCommand ImportFromClipboardCommand { get; }

    public RelayCommand ImportFrameTextCommand { get; }

    public RelayCommand ParseResponseCommand { get; }

    public RelayCommand RefreshSerialPortsCommand { get; }

    public AsyncRelayCommand SendCurrentFrameCommand { get; }

    public RelayCommand ToggleSlaveAddressBaseCommand { get; }

    public RelayCommand ToggleRegisterAddressBaseCommand { get; }

    public RelayCommand ToggleQuantityBaseCommand { get; }

    public RelayCommand ToggleDataBaseCommand { get; }

    public ObservableCollection<ModbusFunctionOption> FunctionOptions { get; }

    public ObservableCollection<string> SerialPortNames { get; } = [];

    public ObservableCollection<string> ParityOptions { get; } = ["None", "Odd", "Even"];

    public ObservableCollection<string> StopBitsOptions { get; } = ["One", "Two"];

    public ObservableCollection<FrameLogEntry> FrameLog { get; } = [];

    public ObservableCollection<string> RecycleBin { get; } = [];

    public ModbusFunctionOption SelectedFunctionOption
    {
        get => _selectedFunctionOption;
        set
        {
            if (value is null || !SetProperty(ref _selectedFunctionOption, value))
            {
                return;
            }

            OnSelectedFunctionChanged();
        }
    }

    public string SlaveAddressText
    {
        get => _slaveAddressText;
        set => UpdateFieldText(FrameFieldKey.SlaveAddress, value);
    }

    public string FunctionCodeText
    {
        get => _functionCodeText;
        private set => SetProperty(ref _functionCodeText, value);
    }

    public string RegisterAddressText
    {
        get => _registerAddressText;
        set => UpdateFieldText(FrameFieldKey.RegisterAddress, value);
    }

    public string QuantityText
    {
        get => _quantityText;
        set => UpdateFieldText(FrameFieldKey.Quantity, value);
    }

    public string DataText
    {
        get => _dataText;
        set => UpdateFieldText(FrameFieldKey.Data, value);
    }

    public string ValuesText
    {
        get => _valuesText;
        set
        {
            string normalizedText = NormalizeValuesText(value);
            bool changed = SetProperty(ref _valuesText, normalizedText);
            if (!changed && !string.Equals(normalizedText, value ?? string.Empty, StringComparison.Ordinal))
            {
                OnPropertyChanged();
            }

            if (!_suppressAutoGenerate && changed)
            {
                GenerateAndCopyFrame();
            }
        }
    }

    public string FrameImportText
    {
        get => _frameImportText;
        set
        {
            string sanitizedText = SanitizeFrameText(value);
            if (!SetProperty(ref _frameImportText, sanitizedText)
                && !string.Equals(sanitizedText, value ?? string.Empty, StringComparison.Ordinal))
            {
                OnPropertyChanged();
            }
        }
    }

    public string FrameImportStatusText
    {
        get => _frameImportStatusText;
        private set => SetProperty(ref _frameImportStatusText, value);
    }

    public bool IsFrameImportStatusVisible
    {
        get => _isFrameImportStatusVisible;
        private set => SetProperty(ref _isFrameImportStatusVisible, value);
    }

    public bool IsFrameImportStatusWarning
    {
        get => _isFrameImportStatusWarning;
        private set => SetProperty(ref _isFrameImportStatusWarning, value);
    }

    public string SlaveAddressBaseText => FormatNumberBase(_slaveAddressBase);

    public string RegisterAddressBaseText => FormatNumberBase(_registerAddressBase);

    public string QuantityBaseText => FormatNumberBase(_quantityBase);

    public string DataBaseText => FormatNumberBase(_dataBase);

    public bool IsQuantityFieldVisible => SelectedFunctionOption.Mode is RequestDataMode.ReadBits
        or RequestDataMode.ReadRegisters;

    public bool IsDataFieldVisible => SelectedFunctionOption.Mode is RequestDataMode.WriteSingleCoil
        or RequestDataMode.WriteSingleRegister;

    public bool IsValuesFieldVisible => SelectedFunctionOption.Mode is RequestDataMode.WriteMultipleCoils
        or RequestDataMode.WriteMultipleRegisters;

    public string AddressFieldLabel => SelectedFunctionOption.Mode is RequestDataMode.ReadBits
        or RequestDataMode.ReadRegisters
        ? "起始地址"
        : "寄存器/线圈地址";

    public string QuantityFieldLabel => SelectedFunctionOption.Mode is RequestDataMode.ReadBits
        ? "线圈数量"
        : "寄存器数量";

    public string QuantityFieldHint => SelectedFunctionOption.Mode is RequestDataMode.ReadBits
        ? "读取数量，最大 2000"
        : "读取数量，最大 125";

    public string DataFieldLabel => SelectedFunctionOption.Mode == RequestDataMode.WriteSingleCoil
        ? "线圈值"
        : "寄存器值";

    public string DataFieldHint => SelectedFunctionOption.Mode == RequestDataMode.WriteSingleCoil
        ? "0/1，发送时转换为 0000 / FF00"
        : "2 字节写入值";

    public string ValuesFieldLabel => SelectedFunctionOption.Mode == RequestDataMode.WriteMultipleCoils
        ? "线圈值列表"
        : "寄存器值列表";

    public string ValuesFieldHint => SelectedFunctionOption.Mode == RequestDataMode.WriteMultipleCoils
        ? "输入 0/1 列表，例如 1 0 1 1"
        : "输入 16 位十六进制列表，例如 0001 0002 03E8";

    public string HeaderStatusText
    {
        get => _headerStatusText;
        private set => SetProperty(ref _headerStatusText, value);
    }

    public string RawFrameText
    {
        get => _rawFrameText;
        private set => SetProperty(ref _rawFrameText, value);
    }

    public string CrcText
    {
        get => _crcText;
        private set => SetProperty(ref _crcText, value);
    }

    public string CopyStatusText
    {
        get => _copyStatusText;
        private set => SetProperty(ref _copyStatusText, value);
    }

    public string NextStepText
    {
        get => _nextStepText;
        private set => SetProperty(ref _nextStepText, value);
    }

    public string ResponseImportText
    {
        get => _responseImportText;
        set => SetProperty(ref _responseImportText, SanitizeFrameText(value));
    }

    public string ResponseSummaryText
    {
        get => _responseSummaryText;
        private set => SetProperty(ref _responseSummaryText, value);
    }

    public string ResponseDetailsText
    {
        get => _responseDetailsText;
        private set => SetProperty(ref _responseDetailsText, value);
    }

    public string ResponseCrcStatusText
    {
        get => _responseCrcStatusText;
        private set => SetProperty(ref _responseCrcStatusText, value);
    }

    public string SerialStatusText
    {
        get => _serialStatusText;
        private set => SetProperty(ref _serialStatusText, value);
    }

    public string SelectedSerialPortName
    {
        get => _selectedSerialPortName;
        set => SetProperty(ref _selectedSerialPortName, value ?? string.Empty);
    }

    public string BaudRateText
    {
        get => _baudRateText;
        set => SetProperty(ref _baudRateText, value ?? string.Empty);
    }

    public string DataBitsText
    {
        get => _dataBitsText;
        set => SetProperty(ref _dataBitsText, value ?? string.Empty);
    }

    public string SelectedParityOption
    {
        get => _selectedParityOption;
        set => SetProperty(ref _selectedParityOption, value ?? "None");
    }

    public string SelectedStopBitsOption
    {
        get => _selectedStopBitsOption;
        set => SetProperty(ref _selectedStopBitsOption, value ?? "One");
    }

    public string SerialTimeoutText
    {
        get => _serialTimeoutText;
        set => SetProperty(ref _serialTimeoutText, value ?? string.Empty);
    }

    private void FillExample()
    {
        _suppressAutoGenerate = true;
        SelectedFunctionOption = FunctionOptions.First(static option => option.Code == 0x06);
        SetFieldBase(FrameFieldKey.SlaveAddress, NumberBase.Dec);
        SetFieldBase(FrameFieldKey.RegisterAddress, NumberBase.Hex);
        SetFieldBase(FrameFieldKey.Quantity, NumberBase.Dec);
        SetFieldBase(FrameFieldKey.Data, NumberBase.Dec);

        SetRawFieldText(FrameFieldKey.SlaveAddress, "10");
        SetRawFieldText(FrameFieldKey.FunctionCode, "06");
        SetRawFieldText(FrameFieldKey.RegisterAddress, "0030");
        SetRawFieldText(FrameFieldKey.Quantity, "1");
        SetRawFieldText(FrameFieldKey.Data, "42330");
        ValuesText = "0001 0002";
        _suppressAutoGenerate = false;

        SetFrameImportStatus(null);
        GenerateAndCopyFrame();
    }

    private void GenerateAndCopyFrame(bool addHistory = false)
    {
        SetFrameImportStatus(null);

        if (!TryGetCurrentInput(out ModbusFrameInput input, out string? errorMessage))
        {
            ShowValidationError(errorMessage ?? "输入参数无效。");
            return;
        }

        ApplyFrameValues(input, "已复制到剪贴板");
        if (addHistory)
        {
            AddFrameLog("生成", RawFrameText, CopyStatusText);
        }
    }

    private async Task SendCurrentFrameAsync()
    {
        if (!TryGetCurrentInput(out ModbusFrameInput input, out string? errorMessage))
        {
            ShowValidationError(errorMessage ?? "输入参数无效。");
            return;
        }

        if (!TryGetSerialSettings(out ModbusSerialSettings settings, out errorMessage))
        {
            SerialStatusText = errorMessage ?? "串口配置无效。";
            return;
        }

        ModbusFrameResult frameResult = ApplyFrameValues(input, "已生成并准备发送");
        AddFrameLog("发送", frameResult.RawFrameDisplay, $"TX {settings.PortName}");
        SerialStatusText = "正在发送...";

        ModbusSerialExchangeResult exchangeResult = await _modbusSerialService.SendAndReceiveAsync(
            frameResult.FrameBytes,
            settings);

        SerialStatusText = $"{exchangeResult.StatusText} ({exchangeResult.Elapsed.TotalMilliseconds:F0} ms)";
        if (!exchangeResult.IsSuccess || exchangeResult.ResponseBytes.Length == 0)
        {
            AddFrameLog("接收", "--", exchangeResult.StatusText);
            return;
        }

        string responseFrame = FormatFrame(exchangeResult.ResponseBytes);
        ResponseImportText = responseFrame;
        ParseResponseCore(responseFrame);
        AddFrameLog("接收", responseFrame, ResponseSummaryText);
    }

    private void ImportFrameFromClipboard()
    {
        AddCurrentImportTextToRecycleBin();

        string clipboardText;
        try
        {
            clipboardText = _clipboardService.ContainsText() ? _clipboardService.GetText() : string.Empty;
        }
        catch (Exception exception)
        {
            ShowValidationError($"读取剪贴板失败：{exception.Message}");
            return;
        }

        FrameImportText = clipboardText;

        if (string.IsNullOrWhiteSpace(clipboardText))
        {
            ShowValidationError("剪贴板为空，无法导入原始帧。");
            return;
        }

        ImportFrameTextCore(clipboardText, "已导入并复制");
    }

    private void ImportFrameFromTextBox()
    {
        string inputText = FrameImportText.Trim();
        if (string.IsNullOrWhiteSpace(inputText))
        {
            ShowValidationError("原始帧输入框为空。");
            return;
        }

        ImportFrameTextCore(inputText, "已复制");
    }

    private void ParseResponseFromTextBox()
    {
        string inputText = ResponseImportText.Trim();
        if (string.IsNullOrWhiteSpace(inputText))
        {
            ResponseSummaryText = "响应帧输入框为空。";
            ResponseDetailsText = string.Empty;
            ResponseCrcStatusText = "--";
            return;
        }

        ParseResponseCore(inputText);
    }

    private void ImportFrameTextCore(string frameText, string successTextWhenNotCorrected)
    {
        FrameImportResult importResult = _modbusFrameService.ImportFrame(frameText);
        if (!importResult.IsSuccess || importResult.Input is null)
        {
            SetFrameImportStatus("无效输入", true);
            ShowValidationError($"原始帧导入失败：{importResult.ErrorMessage}");
            return;
        }

        ApplyImportedInput(importResult.Input);
        SetFrameImportStatus(importResult.IsCorrected ? "纠错成功" : "复制成功");
        ApplyFrameValues(
            importResult.Input,
            importResult.IsCorrected ? "已纠错并复制" : successTextWhenNotCorrected);
    }

    private void ParseResponseCore(string frameText)
    {
        ModbusResponseParseResult parseResult = _modbusFrameService.ParseResponse(frameText);
        if (!parseResult.IsSuccess)
        {
            ResponseSummaryText = parseResult.ErrorMessage ?? "解析失败。";
            ResponseDetailsText = string.Empty;
            ResponseCrcStatusText = "无效";
            return;
        }

        ResponseSummaryText = parseResult.Summary;
        ResponseDetailsText = string.Join(Environment.NewLine, parseResult.Details);
        ResponseCrcStatusText = parseResult.IsCrcValid ? "CRC 正确" : "CRC 错误";
    }

    private bool TryGetCurrentInput(out ModbusFrameInput input, out string? errorMessage)
    {
        input = default!;

        if (!TryParseField(FrameFieldKey.SlaveAddress, "从站地址", byte.MaxValue, out ushort slaveAddress, out errorMessage))
        {
            return false;
        }

        if (!TryParseField(FrameFieldKey.RegisterAddress, AddressFieldLabel, ushort.MaxValue, out ushort registerAddress, out errorMessage))
        {
            return false;
        }

        byte functionCode = SelectedFunctionOption.Code;
        input = SelectedFunctionOption.Mode switch
        {
            RequestDataMode.ReadBits or RequestDataMode.ReadRegisters => CreateQuantityInput(
                (byte)slaveAddress,
                functionCode,
                registerAddress,
                out errorMessage),
            RequestDataMode.WriteSingleCoil => CreateSingleCoilInput(
                (byte)slaveAddress,
                functionCode,
                registerAddress,
                out errorMessage),
            RequestDataMode.WriteSingleRegister => CreateSingleRegisterInput(
                (byte)slaveAddress,
                functionCode,
                registerAddress,
                out errorMessage),
            RequestDataMode.WriteMultipleCoils => CreateMultipleValuesInput(
                (byte)slaveAddress,
                functionCode,
                registerAddress,
                isCoilList: true,
                out errorMessage),
            RequestDataMode.WriteMultipleRegisters => CreateMultipleValuesInput(
                (byte)slaveAddress,
                functionCode,
                registerAddress,
                isCoilList: false,
                out errorMessage),
            _ => default!
        };

        return errorMessage is null;
    }

    private ModbusFrameInput CreateQuantityInput(
        byte slaveAddress,
        byte functionCode,
        ushort registerAddress,
        out string? errorMessage)
    {
        uint maxQuantity = SelectedFunctionOption.Mode == RequestDataMode.ReadBits ? 2000u : 125u;
        if (!TryParseField(FrameFieldKey.Quantity, QuantityFieldLabel, maxQuantity, out ushort quantity, out errorMessage))
        {
            return default!;
        }

        if (quantity == 0)
        {
            errorMessage = $"{QuantityFieldLabel}错误：数量必须大于 0。";
            return default!;
        }

        errorMessage = null;
        return new ModbusFrameInput(slaveAddress, functionCode, registerAddress, 0, quantity);
    }

    private ModbusFrameInput CreateSingleCoilInput(
        byte slaveAddress,
        byte functionCode,
        ushort registerAddress,
        out string? errorMessage)
    {
        if (!TryParseField(FrameFieldKey.Data, DataFieldLabel, ushort.MaxValue, out ushort coilValue, out errorMessage))
        {
            return default!;
        }

        if (coilValue is not 0 and not 1 and not 0xFF00)
        {
            errorMessage = "线圈值错误：只能输入 0、1 或 FF00。";
            return default!;
        }

        errorMessage = null;
        return new ModbusFrameInput(slaveAddress, functionCode, registerAddress, coilValue == 0 ? (ushort)0 : (ushort)0xFF00);
    }

    private ModbusFrameInput CreateSingleRegisterInput(
        byte slaveAddress,
        byte functionCode,
        ushort registerAddress,
        out string? errorMessage)
    {
        if (!TryParseField(FrameFieldKey.Data, DataFieldLabel, ushort.MaxValue, out ushort dataValue, out errorMessage))
        {
            return default!;
        }

        errorMessage = null;
        return new ModbusFrameInput(slaveAddress, functionCode, registerAddress, dataValue);
    }

    private ModbusFrameInput CreateMultipleValuesInput(
        byte slaveAddress,
        byte functionCode,
        ushort registerAddress,
        bool isCoilList,
        out string? errorMessage)
    {
        if (!TryParseValues(isCoilList, out ushort[] values, out errorMessage))
        {
            return default!;
        }

        uint maxQuantity = isCoilList ? 2000u : 123u;
        if (values.Length == 0 || values.Length > maxQuantity)
        {
            errorMessage = isCoilList
                ? "线圈值列表错误：数量必须为 1-2000。"
                : "寄存器值列表错误：数量必须为 1-123。";
            return default!;
        }

        errorMessage = null;
        return new ModbusFrameInput(slaveAddress, functionCode, registerAddress, 0, (ushort)values.Length, values);
    }

    private bool TryParseField(
        FrameFieldKey fieldKey,
        string label,
        uint maxValue,
        out ushort value,
        out string? errorMessage)
    {
        if (!_modbusFrameService.TryParseFieldValue(
            GetFieldText(fieldKey),
            GetFieldBase(fieldKey),
            maxValue,
            out uint parsedValue,
            out string? parseError))
        {
            value = 0;
            errorMessage = $"{label}错误：{parseError}";
            return false;
        }

        value = (ushort)parsedValue;
        errorMessage = null;
        return true;
    }

    private bool TryParseValues(bool isCoilList, out ushort[] values, out string? errorMessage)
    {
        values = [];
        string input = ValuesText.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            errorMessage = $"{ValuesFieldLabel}错误：请输入至少一个值。";
            return false;
        }

        string[] tokens = SplitValueTokens(input, isCoilList);
        var parsedValues = new List<ushort>();
        foreach (string token in tokens)
        {
            if (isCoilList)
            {
                if (token is "0")
                {
                    parsedValues.Add(0);
                    continue;
                }

                if (token is "1")
                {
                    parsedValues.Add(1);
                    continue;
                }

                errorMessage = "线圈值列表错误：只能输入 0 或 1。";
                return false;
            }

            string cleaned = token.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? token[2..] : token;
            if (cleaned.Length is 0 or > 4 || cleaned.Any(static c => !Uri.IsHexDigit(c)))
            {
                errorMessage = "寄存器值列表错误：每个值必须是 1-4 位十六进制。";
                return false;
            }

            parsedValues.Add(ushort.Parse(cleaned, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture));
        }

        values = [.. parsedValues];
        errorMessage = null;
        return true;
    }

    private bool TryGetSerialSettings(out ModbusSerialSettings settings, out string? errorMessage)
    {
        settings = default!;
        if (string.IsNullOrWhiteSpace(SelectedSerialPortName))
        {
            errorMessage = "请先选择串口。";
            return false;
        }

        if (!TryParseInt(BaudRateText, 300, 1000000, "波特率", out int baudRate, out errorMessage)
            || !TryParseInt(DataBitsText, 5, 8, "数据位", out int dataBits, out errorMessage)
            || !TryParseInt(SerialTimeoutText, 100, 60000, "超时", out int timeout, out errorMessage))
        {
            return false;
        }

        settings = new ModbusSerialSettings(
            SelectedSerialPortName,
            baudRate,
            dataBits,
            SelectedParityOption,
            SelectedStopBitsOption,
            timeout);
        errorMessage = null;
        return true;
    }

    private static bool TryParseInt(
        string text,
        int minValue,
        int maxValue,
        string label,
        out int value,
        out string? errorMessage)
    {
        if (!int.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out value)
            || value < minValue
            || value > maxValue)
        {
            errorMessage = $"{label}必须为 {minValue}-{maxValue}。";
            return false;
        }

        errorMessage = null;
        return true;
    }

    private ModbusFrameResult ApplyFrameValues(ModbusFrameInput input, string headerStatus)
    {
        ModbusFrameResult result = _modbusFrameService.BuildFrame(input);
        _currentFrameBytes = result.FrameBytes;

        RawFrameText = result.RawFrameDisplay;
        CrcText = result.Crc.ToString("X4", CultureInfo.InvariantCulture);

        try
        {
            _clipboardService.SetText(result.ClipboardFrame);
            HeaderStatusText = headerStatus;
            CopyStatusText = "复制成功";
            NextStepText = "可直接复制发送，也可以选择串口后点击“发送”。";
        }
        catch (Exception exception)
        {
            HeaderStatusText = "已生成，复制失败";
            CopyStatusText = "复制失败";
            NextStepText = $"原始帧已生成，但写入剪贴板失败：{exception.Message}";
        }

        return result;
    }

    private void ApplyImportedInput(ModbusFrameInput input)
    {
        _suppressAutoGenerate = true;
        SetSelectedFunction(input.FunctionCode);
        SetFieldText(FrameFieldKey.SlaveAddress, input.SlaveAddress);
        SetFieldText(FrameFieldKey.RegisterAddress, input.RegisterAddress);
        SetFieldText(FrameFieldKey.Quantity, input.Quantity);
        SetFieldText(FrameFieldKey.Data, input.DataValue == 0xFF00 && input.FunctionCode == 0x05 ? 1u : input.DataValue);
        ValuesText = input.Values is { Count: > 0 }
            ? FormatValuesText(input.FunctionCode, input.Values)
            : ValuesText;
        _suppressAutoGenerate = false;
    }

    private void ShowValidationError(string message)
    {
        _currentFrameBytes = [];
        HeaderStatusText = "等待有效输入";
        RawFrameText = string.Empty;
        CrcText = "--";
        CopyStatusText = "未复制";
        NextStepText = message;
    }

    private void ToggleFieldBase(FrameFieldKey fieldKey)
    {
        NumberBase currentBase = GetFieldBase(fieldKey);
        NumberBase targetBase = currentBase == NumberBase.Hex ? NumberBase.Dec : NumberBase.Hex;

        if (!_modbusFrameService.TryParseFieldValue(
            GetFieldText(fieldKey),
            currentBase,
            _modbusFrameService.GetFieldMaxValue(fieldKey),
            out uint value,
            out _))
        {
            // 当前内容无法转换时，切换模式并清空该字段，避免误读旧输入。
            _suppressAutoGenerate = true;
            SetFieldBase(fieldKey, targetBase);
            SetRawFieldText(fieldKey, string.Empty);
            _suppressAutoGenerate = false;
            GenerateAndCopyFrame();
            return;
        }

        _suppressAutoGenerate = true;
        SetFieldBase(fieldKey, targetBase);
        SetRawFieldText(fieldKey, _modbusFrameService.FormatFieldValue(fieldKey, value, targetBase));
        _suppressAutoGenerate = false;

        GenerateAndCopyFrame();
    }

    private void UpdateFieldText(FrameFieldKey fieldKey, string? value)
    {
        if (_suppressAutoGenerate)
        {
            SetRawFieldText(fieldKey, value ?? string.Empty);
            return;
        }

        string normalizedText = NormalizeFieldTextInput(fieldKey, value ?? string.Empty, out bool baseChanged);
        bool textChanged = SetRawFieldText(fieldKey, normalizedText);
        if (!textChanged && !string.Equals(normalizedText, value ?? string.Empty, StringComparison.Ordinal))
        {
            OnPropertyChanged(GetFieldPropertyName(fieldKey));
        }

        if (textChanged || baseChanged)
        {
            GenerateAndCopyFrame();
        }
    }

    private void OnSelectedFunctionChanged()
    {
        _suppressAutoGenerate = true;
        SetRawFieldText(FrameFieldKey.FunctionCode, SelectedFunctionOption.Code.ToString("X2", CultureInfo.InvariantCulture));
        EnsureFunctionDefaults();
        NotifyFunctionShapeChanged();
        _suppressAutoGenerate = false;
        GenerateAndCopyFrame();
    }

    private void EnsureFunctionDefaults()
    {
        if (string.IsNullOrWhiteSpace(QuantityText))
        {
            SetRawFieldText(FrameFieldKey.Quantity, "1");
        }

        if (SelectedFunctionOption.Mode == RequestDataMode.WriteSingleCoil)
        {
            SetFieldBase(FrameFieldKey.Data, NumberBase.Dec);
            SetRawFieldText(FrameFieldKey.Data, string.IsNullOrWhiteSpace(DataText) ? "1" : DataText);
        }
        else if (SelectedFunctionOption.Mode == RequestDataMode.WriteSingleRegister && string.IsNullOrWhiteSpace(DataText))
        {
            SetRawFieldText(FrameFieldKey.Data, "1");
        }
        else if (SelectedFunctionOption.Mode == RequestDataMode.WriteMultipleCoils && string.IsNullOrWhiteSpace(ValuesText))
        {
            ValuesText = "1 0 1";
        }
        else if (SelectedFunctionOption.Mode == RequestDataMode.WriteMultipleRegisters && string.IsNullOrWhiteSpace(ValuesText))
        {
            ValuesText = "0001 0002";
        }
    }

    private void NotifyFunctionShapeChanged()
    {
        OnPropertyChanged(nameof(IsQuantityFieldVisible));
        OnPropertyChanged(nameof(IsDataFieldVisible));
        OnPropertyChanged(nameof(IsValuesFieldVisible));
        OnPropertyChanged(nameof(AddressFieldLabel));
        OnPropertyChanged(nameof(QuantityFieldLabel));
        OnPropertyChanged(nameof(QuantityFieldHint));
        OnPropertyChanged(nameof(DataFieldLabel));
        OnPropertyChanged(nameof(DataFieldHint));
        OnPropertyChanged(nameof(ValuesFieldLabel));
        OnPropertyChanged(nameof(ValuesFieldHint));
    }

    private string NormalizeFieldTextInput(FrameFieldKey fieldKey, string input, out bool baseChanged)
    {
        baseChanged = false;
        string cleaned = input.Trim().Replace(" ", string.Empty);
        if (cleaned.Length == 0)
        {
            return cleaned;
        }

        if (cleaned.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[2..].ToUpperInvariant();
            baseChanged = SetFieldBase(fieldKey, NumberBase.Hex);
            return cleaned;
        }

        if (cleaned.StartsWith("DEC", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[3..];
            baseChanged = SetFieldBase(fieldKey, NumberBase.Dec);
            return cleaned;
        }

        if (cleaned.StartsWith("0d", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[2..];
            baseChanged = SetFieldBase(fieldKey, NumberBase.Dec);
            return cleaned;
        }

        return GetFieldBase(fieldKey) == NumberBase.Hex
            ? cleaned.ToUpperInvariant()
            : cleaned;
    }

    private void SetSelectedFunction(byte functionCode)
    {
        ModbusFunctionOption? option = FunctionOptions.FirstOrDefault(item => item.Code == functionCode);
        if (option is null)
        {
            return;
        }

        _selectedFunctionOption = option;
        OnPropertyChanged(nameof(SelectedFunctionOption));
        SetRawFieldText(FrameFieldKey.FunctionCode, functionCode.ToString("X2", CultureInfo.InvariantCulture));
        NotifyFunctionShapeChanged();
    }

    private void SetFieldText(FrameFieldKey fieldKey, uint value)
    {
        SetRawFieldText(fieldKey, _modbusFrameService.FormatFieldValue(fieldKey, value, GetFieldBase(fieldKey)));
    }

    private bool SetRawFieldText(FrameFieldKey fieldKey, string value)
    {
        return fieldKey switch
        {
            FrameFieldKey.SlaveAddress => SetProperty(ref _slaveAddressText, value, nameof(SlaveAddressText)),
            FrameFieldKey.FunctionCode => SetProperty(ref _functionCodeText, value, nameof(FunctionCodeText)),
            FrameFieldKey.RegisterAddress => SetProperty(ref _registerAddressText, value, nameof(RegisterAddressText)),
            FrameFieldKey.Quantity => SetProperty(ref _quantityText, value, nameof(QuantityText)),
            FrameFieldKey.Data => SetProperty(ref _dataText, value, nameof(DataText)),
            _ => false
        };
    }

    private string GetFieldText(FrameFieldKey fieldKey)
    {
        return fieldKey switch
        {
            FrameFieldKey.SlaveAddress => SlaveAddressText,
            FrameFieldKey.FunctionCode => FunctionCodeText,
            FrameFieldKey.RegisterAddress => RegisterAddressText,
            FrameFieldKey.Quantity => QuantityText,
            FrameFieldKey.Data => DataText,
            _ => string.Empty
        };
    }

    private static string GetFieldPropertyName(FrameFieldKey fieldKey)
    {
        return fieldKey switch
        {
            FrameFieldKey.SlaveAddress => nameof(SlaveAddressText),
            FrameFieldKey.FunctionCode => nameof(FunctionCodeText),
            FrameFieldKey.RegisterAddress => nameof(RegisterAddressText),
            FrameFieldKey.Quantity => nameof(QuantityText),
            FrameFieldKey.Data => nameof(DataText),
            _ => string.Empty
        };
    }

    private NumberBase GetFieldBase(FrameFieldKey fieldKey)
    {
        return fieldKey switch
        {
            FrameFieldKey.SlaveAddress => _slaveAddressBase,
            FrameFieldKey.RegisterAddress => _registerAddressBase,
            FrameFieldKey.Quantity => _quantityBase,
            FrameFieldKey.Data => _dataBase,
            _ => NumberBase.Hex
        };
    }

    private bool SetFieldBase(FrameFieldKey fieldKey, NumberBase numberBase)
    {
        return fieldKey switch
        {
            FrameFieldKey.SlaveAddress => SetProperty(ref _slaveAddressBase, numberBase, nameof(SlaveAddressBaseText)),
            FrameFieldKey.RegisterAddress => SetProperty(ref _registerAddressBase, numberBase, nameof(RegisterAddressBaseText)),
            FrameFieldKey.Quantity => SetProperty(ref _quantityBase, numberBase, nameof(QuantityBaseText)),
            FrameFieldKey.Data => SetProperty(ref _dataBase, numberBase, nameof(DataBaseText)),
            _ => false
        };
    }

    private void RefreshSerialPorts()
    {
        try
        {
            string previousPort = SelectedSerialPortName;
            SerialPortNames.Clear();
            foreach (string portName in _modbusSerialService.GetPortNames())
            {
                SerialPortNames.Add(portName);
            }

            SelectedSerialPortName = SerialPortNames.Contains(previousPort)
                ? previousPort
                : SerialPortNames.FirstOrDefault() ?? string.Empty;
            SerialStatusText = SerialPortNames.Count == 0 ? "未发现串口" : $"发现 {SerialPortNames.Count} 个串口";
        }
        catch (Exception exception)
        {
            SerialStatusText = $"刷新串口失败：{exception.Message}";
        }
    }

    private void AddFrameLog(string direction, string frameText, string statusText)
    {
        FrameLog.Insert(0, new FrameLogEntry(
            DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            direction,
            frameText,
            statusText));

        while (FrameLog.Count > 80)
        {
            FrameLog.RemoveAt(FrameLog.Count - 1);
        }
    }

    private void SetFrameImportStatus(string? text, bool isWarning = false)
    {
        FrameImportStatusText = text ?? string.Empty;
        IsFrameImportStatusWarning = isWarning;
        IsFrameImportStatusVisible = !string.IsNullOrWhiteSpace(text);
    }

    private void AddCurrentImportTextToRecycleBin()
    {
        string existingText = FrameImportText.Trim();
        if (string.IsNullOrWhiteSpace(existingText))
        {
            return;
        }

        if (RecycleBin.Count > 0 && string.Equals(RecycleBin[^1], existingText, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        RecycleBin.Add(existingText);
    }

    private static string NormalizeValuesText(string? value)
    {
        return (value ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static string SanitizeFrameText(string? value)
    {
        string cleaned = new((value ?? string.Empty)
            .Where(static c => !char.IsWhiteSpace(c) && c is not '-' and not ',' and not ':')
            .ToArray());

        return cleaned.Replace("0x", string.Empty, StringComparison.OrdinalIgnoreCase).ToUpperInvariant();
    }

    private static string[] SplitValueTokens(string input, bool isCoilList)
    {
        char[] separators = [' ', '\t', '\r', '\n', ',', ';', '|'];
        string[] tokens = input.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length > 1)
        {
            return tokens;
        }

        string singleToken = tokens.Length == 1 ? tokens[0] : input;
        if (isCoilList && singleToken.All(static c => c is '0' or '1') && singleToken.Length > 1)
        {
            return singleToken.Select(static c => c.ToString()).ToArray();
        }

        string cleaned = singleToken.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? singleToken[2..] : singleToken;
        if (!isCoilList && cleaned.Length > 4 && cleaned.Length % 4 == 0 && cleaned.All(Uri.IsHexDigit))
        {
            return Enumerable.Range(0, cleaned.Length / 4)
                .Select(i => cleaned.Substring(i * 4, 4))
                .ToArray();
        }

        return [singleToken];
    }

    private static string FormatValuesText(byte functionCode, IReadOnlyList<ushort> values)
    {
        return functionCode == 0x0F
            ? string.Join(" ", values.Select(static value => value == 0 ? "0" : "1"))
            : string.Join(" ", values.Select(static value => value.ToString("X4", CultureInfo.InvariantCulture)));
    }

    private static string FormatFrame(byte[] frame)
    {
        return string.Join(" ", frame.Select(static b => b.ToString("X2", CultureInfo.InvariantCulture)));
    }

    private static string FormatNumberBase(NumberBase numberBase)
    {
        return numberBase == NumberBase.Hex ? "0x" : "DEC";
    }
}

public sealed record ModbusFunctionOption(byte Code, string Name, RequestDataMode Mode)
{
    public string DisplayName => $"0x{Code:X2} {Name}";
}

public sealed record FrameLogEntry(string TimeText, string Direction, string FrameText, string StatusText);

public enum RequestDataMode
{
    ReadBits,
    ReadRegisters,
    WriteSingleCoil,
    WriteSingleRegister,
    WriteMultipleCoils,
    WriteMultipleRegisters
}
