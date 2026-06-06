using System.Collections.ObjectModel;
using Project.Core;
using Project.Models;

namespace Project.ViewModels;

public sealed class ModbusCrcViewModel : ObservableObject
{
    private readonly IModbusFrameService _modbusFrameService;
    private readonly IClipboardService _clipboardService;

    private bool _suppressAutoGenerate;
    private string _slaveAddressText = string.Empty;
    private string _functionCodeText = string.Empty;
    private string _registerAddressText = string.Empty;
    private string _dataText = string.Empty;
    private string _frameImportText = string.Empty;
    private string _frameImportStatusText = string.Empty;
    private bool _isFrameImportStatusVisible;
    private bool _isFrameImportStatusWarning;
    private NumberBase _slaveAddressBase = NumberBase.Dec;
    private NumberBase _functionCodeBase = NumberBase.Hex;
    private NumberBase _registerAddressBase = NumberBase.Hex;
    private NumberBase _dataBase = NumberBase.Dec;
    private string _headerStatusText = "等待输入";
    private string _rawFrameText = string.Empty;
    private string _crcText = "--";
    private string _copyStatusText = "等待生成";
    private string _nextStepText = "先输入完整参数，然后点击“生成并复制”。";

    public ModbusCrcViewModel(IModbusFrameService modbusFrameService, IClipboardService clipboardService)
    {
        _modbusFrameService = modbusFrameService;
        _clipboardService = clipboardService;

        GenerateCommand = new RelayCommand(GenerateAndCopyFrame);
        FillExampleCommand = new RelayCommand(FillExample);
        ImportFromClipboardCommand = new RelayCommand(ImportFrameFromClipboard);
        ImportFrameTextCommand = new RelayCommand(ImportFrameFromTextBox);
        ToggleSlaveAddressBaseCommand = new RelayCommand(() => ToggleFieldBase(FrameFieldKey.SlaveAddress));
        ToggleFunctionCodeBaseCommand = new RelayCommand(() => ToggleFieldBase(FrameFieldKey.FunctionCode));
        ToggleRegisterAddressBaseCommand = new RelayCommand(() => ToggleFieldBase(FrameFieldKey.RegisterAddress));
        ToggleDataBaseCommand = new RelayCommand(() => ToggleFieldBase(FrameFieldKey.Data));

        FillExample();
    }

    public RelayCommand GenerateCommand { get; }

    public RelayCommand FillExampleCommand { get; }

    public RelayCommand ImportFromClipboardCommand { get; }

    public RelayCommand ImportFrameTextCommand { get; }

    public RelayCommand ToggleSlaveAddressBaseCommand { get; }

    public RelayCommand ToggleFunctionCodeBaseCommand { get; }

    public RelayCommand ToggleRegisterAddressBaseCommand { get; }

    public RelayCommand ToggleDataBaseCommand { get; }

    public ObservableCollection<string> RecycleBin { get; } = [];

    public string SlaveAddressText
    {
        get => _slaveAddressText;
        set => UpdateFieldText(FrameFieldKey.SlaveAddress, value);
    }

    public string FunctionCodeText
    {
        get => _functionCodeText;
        set => UpdateFieldText(FrameFieldKey.FunctionCode, value);
    }

    public string RegisterAddressText
    {
        get => _registerAddressText;
        set => UpdateFieldText(FrameFieldKey.RegisterAddress, value);
    }

    public string DataText
    {
        get => _dataText;
        set => UpdateFieldText(FrameFieldKey.Data, value);
    }

    public string FrameImportText
    {
        get => _frameImportText;
        set
        {
            string sanitizedText = SanitizeFrameImportText(value);
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

    public string FunctionCodeBaseText => FormatNumberBase(_functionCodeBase);

    public string RegisterAddressBaseText => FormatNumberBase(_registerAddressBase);

    public string DataBaseText => FormatNumberBase(_dataBase);

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

    private void FillExample()
    {
        _suppressAutoGenerate = true;
        SetFieldBase(FrameFieldKey.SlaveAddress, NumberBase.Dec);
        SetFieldBase(FrameFieldKey.FunctionCode, NumberBase.Hex);
        SetFieldBase(FrameFieldKey.RegisterAddress, NumberBase.Hex);
        SetFieldBase(FrameFieldKey.Data, NumberBase.Dec);

        SetRawFieldText(FrameFieldKey.SlaveAddress, "10");
        SetRawFieldText(FrameFieldKey.FunctionCode, "06");
        SetRawFieldText(FrameFieldKey.RegisterAddress, "0030");
        SetRawFieldText(FrameFieldKey.Data, "42330");
        _suppressAutoGenerate = false;

        SetFrameImportStatus(null);
        GenerateAndCopyFrame();
    }

    private void GenerateAndCopyFrame()
    {
        SetFrameImportStatus(null);

        if (!TryGetCurrentInput(out ModbusFrameInput input, out string? errorMessage))
        {
            ShowValidationError(errorMessage ?? "输入参数无效。");
            return;
        }

        ApplyFrameValues(input, "已复制到剪贴板");
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

    private void ImportFrameTextCore(string frameText, string successTextWhenNotCorrected)
    {
        FrameImportResult importResult = _modbusFrameService.ImportFrame(frameText);
        if (!importResult.IsSuccess || importResult.Input is null)
        {
            SetFrameImportStatus("无效输入", true);
            ShowValidationError($"原始帧导入失败：{importResult.ErrorMessage}");
            return;
        }

        _suppressAutoGenerate = true;
        SetFieldText(FrameFieldKey.SlaveAddress, importResult.Input.SlaveAddress);
        SetFieldText(FrameFieldKey.FunctionCode, importResult.Input.FunctionCode);
        SetFieldText(FrameFieldKey.RegisterAddress, importResult.Input.RegisterAddress);
        SetFieldText(FrameFieldKey.Data, importResult.Input.DataValue);
        _suppressAutoGenerate = false;

        SetFrameImportStatus(importResult.IsCorrected ? "纠错成功" : "复制成功");
        ApplyFrameValues(
            importResult.Input,
            importResult.IsCorrected ? "已纠错并复制" : successTextWhenNotCorrected);
    }

    private bool TryGetCurrentInput(out ModbusFrameInput input, out string? errorMessage)
    {
        input = default!;

        if (!TryParseField(FrameFieldKey.SlaveAddress, "从站地址", out byte slaveAddress, out errorMessage))
        {
            return false;
        }

        if (!TryParseField(FrameFieldKey.FunctionCode, "功能码", out byte functionCode, out errorMessage))
        {
            return false;
        }

        if (!TryParseField(FrameFieldKey.RegisterAddress, "寄存器地址", out ushort registerAddress, out errorMessage))
        {
            return false;
        }

        if (!TryParseField(FrameFieldKey.Data, "数据", out ushort dataValue, out errorMessage))
        {
            return false;
        }

        input = new ModbusFrameInput(slaveAddress, functionCode, registerAddress, dataValue);
        errorMessage = null;
        return true;
    }

    private bool TryParseField(FrameFieldKey fieldKey, string label, out byte value, out string? errorMessage)
    {
        if (!_modbusFrameService.TryParseFieldValue(
            GetFieldText(fieldKey),
            GetFieldBase(fieldKey),
            byte.MaxValue,
            out uint parsedValue,
            out string? parseError))
        {
            value = 0;
            errorMessage = $"{label}错误：{parseError}";
            return false;
        }

        value = (byte)parsedValue;
        errorMessage = null;
        return true;
    }

    private bool TryParseField(FrameFieldKey fieldKey, string label, out ushort value, out string? errorMessage)
    {
        if (!_modbusFrameService.TryParseFieldValue(
            GetFieldText(fieldKey),
            GetFieldBase(fieldKey),
            ushort.MaxValue,
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

    private void ApplyFrameValues(ModbusFrameInput input, string headerStatus)
    {
        ModbusFrameResult result = _modbusFrameService.BuildFrame(input);

        RawFrameText = result.RawFrameDisplay;
        CrcText = result.Crc.ToString("X4");

        try
        {
            _clipboardService.SetText(result.ClipboardFrame);
            HeaderStatusText = headerStatus;
            CopyStatusText = "复制成功";
            NextStepText = "下一步：打开有人云平台的“网络调试”，将剪贴板中的原始帧直接粘贴发送。发送后根据返回帧确认写入是否成功。";
        }
        catch (Exception exception)
        {
            HeaderStatusText = "已生成，复制失败";
            CopyStatusText = "复制失败";
            NextStepText = $"原始帧已生成，但写入剪贴板失败：{exception.Message}";
        }
    }

    private void ShowValidationError(string message)
    {
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
            FrameFieldKey.Data => nameof(DataText),
            _ => string.Empty
        };
    }

    private NumberBase GetFieldBase(FrameFieldKey fieldKey)
    {
        return fieldKey switch
        {
            FrameFieldKey.SlaveAddress => _slaveAddressBase,
            FrameFieldKey.FunctionCode => _functionCodeBase,
            FrameFieldKey.RegisterAddress => _registerAddressBase,
            FrameFieldKey.Data => _dataBase,
            _ => NumberBase.Hex
        };
    }

    private bool SetFieldBase(FrameFieldKey fieldKey, NumberBase numberBase)
    {
        bool changed = fieldKey switch
        {
            FrameFieldKey.SlaveAddress => SetProperty(ref _slaveAddressBase, numberBase, nameof(SlaveAddressBaseText)),
            FrameFieldKey.FunctionCode => SetProperty(ref _functionCodeBase, numberBase, nameof(FunctionCodeBaseText)),
            FrameFieldKey.RegisterAddress => SetProperty(ref _registerAddressBase, numberBase, nameof(RegisterAddressBaseText)),
            FrameFieldKey.Data => SetProperty(ref _dataBase, numberBase, nameof(DataBaseText)),
            _ => false
        };

        return changed;
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

    private static string SanitizeFrameImportText(string? value)
    {
        string cleaned = new string((value ?? string.Empty)
            .Where(static c => !char.IsWhiteSpace(c) && c is not '-' and not ',')
            .ToArray());

        if (cleaned.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[2..];
        }

        if (cleaned.Length > 16)
        {
            cleaned = cleaned[..16];
        }

        return cleaned.ToUpperInvariant();
    }

    private static string FormatNumberBase(NumberBase numberBase)
    {
        return numberBase == NumberBase.Hex ? "0x" : "DEC";
    }
}
