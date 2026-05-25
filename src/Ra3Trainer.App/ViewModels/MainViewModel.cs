using System.Collections.ObjectModel;
using System.IO;
using Microsoft.Win32;
using System.Windows;
using Ra3Trainer.App.Hotkeys;
using Ra3Trainer.App.Views;
using Ra3Trainer.Core.Features;
using Ra3Trainer.Core.Hotkeys;
using Ra3Trainer.Core.Manifest;
using Ra3Trainer.Core.Memory;
using Ra3Trainer.Core.Runtime;

namespace Ra3Trainer.App.ViewModels;

public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private readonly TrainerManifest _manifest;
    private readonly TrainerAppSettingsStore _settingsStore;
    private readonly TrainerProcessLocator _locator = new();
    private readonly GameLauncher _launcher = new();
    private readonly LowLevelKeyboardHook _keyboardHook = new();
    private readonly ForegroundWindowProcess _foregroundWindowProcess = new();
    private readonly HotkeyFeatureDispatcher _hotkeyDispatcher = new();
    private IReadOnlyList<ReinforcementUnitEntry> _reinforcementUnits = ReinforcementUnitCatalog.LoadBuiltIn();
    private Win32ProcessMemory? _memory;
    private TrainerSession? _session;
    private int? _targetProcessId;
    private string _statusMessage = "未检测进程。";
    private string _launcherPath;
    private string _launcherArguments;
    private string _reinforcementUnitIdText = $"0x{ReinforcementSettings.DefaultUnitId:X8}";
    private string _reinforcementCountText = ReinforcementSettings.DefaultCount.ToString();
    private string _reinforcementRankText = ReinforcementSettings.DefaultRank.ToString();
    private string _presetNameText = string.Empty;
    private string _moneyAmountText;
    private string _powerValueText;
    private string _scPointValueText;
    private ReinforcementPreset? _selectedReinforcementPreset;
    private readonly int _attachTimeoutSeconds;
    private bool _arePatchesInstalled;
    private bool _isBusy;
    private bool _isQueueRunning;

    private MainViewModel(TrainerManifest manifest, TrainerAppSettingsStore settingsStore)
    {
        _manifest = manifest;
        _settingsStore = settingsStore;
        var settings = settingsStore.Load();
        _launcherPath = settings.LauncherPath;
        _launcherArguments = settings.LauncherArguments;
        _attachTimeoutSeconds = settings.AttachTimeoutSeconds;
        _moneyAmountText = settings.ResourceValues.MoneyAmount.ToString();
        _powerValueText = settings.ResourceValues.PowerValue.ToString();
        _scPointValueText = settings.ResourceValues.ScPointValue.ToString();
        Groups = new ObservableCollection<FeatureGroupViewModel>(
            CreateGroups(TrainerFeatureCatalog.CreateUiFeatures(manifest.Features)));
        ReinforcementPresets = new ObservableCollection<ReinforcementPreset>(settings.ReinforcementPresets);
        ReinforcementQueue = new ObservableCollection<ReinforcementQueueItemViewModel>();
        ReinforcementQueue.CollectionChanged += (_, _) => RaiseCommandStates();
        RefreshCommand = new RelayCommand(RefreshProcess);
        BrowseLauncherCommand = new RelayCommand(BrowseLauncherPath, () => !IsBusy);
        SaveLauncherSettingsCommand = new RelayCommand(SaveLauncherSettings);
        LaunchAndLoadCommand = new RelayCommand(() => _ = LaunchAndLoadAsync(), () => !IsBusy);
        OpenReinforcementUnitPickerCommand = new RelayCommand(OpenReinforcementUnitPicker, () => !IsBusy);
        SaveReinforcementPresetCommand = new RelayCommand(SaveReinforcementPreset, () => !IsQueueRunning);
        ApplyReinforcementPresetCommand = new RelayCommand(ApplySelectedReinforcementPreset, () => SelectedReinforcementPreset is not null && !IsQueueRunning);
        AddSelectedPresetToQueueCommand = new RelayCommand(AddSelectedPresetToQueue, () => SelectedReinforcementPreset is not null && !IsQueueRunning);
        AddCurrentReinforcementToQueueCommand = new RelayCommand(AddCurrentReinforcementToQueue, () => !IsQueueRunning);
        ExecuteReinforcementQueueCommand = new RelayCommand(() => _ = ExecuteReinforcementQueueAsync(), () => ArePatchesInstalled && !IsQueueRunning && ReinforcementQueue.Count > 0);
        ClearReinforcementQueueCommand = new RelayCommand(ClearReinforcementQueue, () => !IsQueueRunning && ReinforcementQueue.Count > 0);
        InstallPatchesCommand = new RelayCommand(InstallPatches, () => _session?.CanUseFeatures == true && !ArePatchesInstalled);
        RestorePatchesCommand = new RelayCommand(RestorePatches);
        ReadSelectedUnitCodeCommand = new RelayCommand(ReadSelectedUnitCode, () => FeatureController is not null);
        _keyboardHook.KeyDown += OnKeyboardHookKeyDown;
        _keyboardHook.KeyUp += OnKeyboardHookKeyUp;
    }

    public static MainViewModel LoadDefault()
    {
        return new MainViewModel(TrainerRuntimeAssets.LoadManifest(), new TrainerAppSettingsStore());
    }

    public ObservableCollection<FeatureGroupViewModel> Groups { get; }

    public ObservableCollection<ReinforcementPreset> ReinforcementPresets { get; }

    public ObservableCollection<ReinforcementQueueItemViewModel> ReinforcementQueue { get; }

    public RelayCommand RefreshCommand { get; }

    public RelayCommand BrowseLauncherCommand { get; }

    public RelayCommand SaveLauncherSettingsCommand { get; }

    public RelayCommand LaunchAndLoadCommand { get; }

    public RelayCommand OpenReinforcementUnitPickerCommand { get; }

    public RelayCommand SaveReinforcementPresetCommand { get; }

    public RelayCommand ApplyReinforcementPresetCommand { get; }

    public RelayCommand AddSelectedPresetToQueueCommand { get; }

    public RelayCommand AddCurrentReinforcementToQueueCommand { get; }

    public RelayCommand ExecuteReinforcementQueueCommand { get; }

    public RelayCommand ClearReinforcementQueueCommand { get; }

    public RelayCommand InstallPatchesCommand { get; }

    public RelayCommand RestorePatchesCommand { get; }

    public RelayCommand ReadSelectedUnitCodeCommand { get; }

    public FeatureController? FeatureController { get; private set; }

    public bool ArePatchesInstalled
    {
        get => _arePatchesInstalled;
        private set
        {
            _arePatchesInstalled = value;
            OnPropertyChanged();
            RaiseFeatureCommandStates();
        }
    }

    public string LauncherPath
    {
        get => _launcherPath;
        set
        {
            _launcherPath = value;
            OnPropertyChanged();
        }
    }

    public string LauncherArguments
    {
        get => _launcherArguments;
        set
        {
            _launcherArguments = value;
            OnPropertyChanged();
        }
    }

    public string ReinforcementUnitIdText
    {
        get => _reinforcementUnitIdText;
        set
        {
            _reinforcementUnitIdText = value;
            OnPropertyChanged();
        }
    }

    public string ReinforcementCountText
    {
        get => _reinforcementCountText;
        set
        {
            _reinforcementCountText = value;
            OnPropertyChanged();
        }
    }

    public string ReinforcementRankText
    {
        get => _reinforcementRankText;
        set
        {
            _reinforcementRankText = value;
            OnPropertyChanged();
        }
    }

    public string PresetNameText
    {
        get => _presetNameText;
        set
        {
            _presetNameText = value;
            OnPropertyChanged();
        }
    }

    public string MoneyAmountText
    {
        get => _moneyAmountText;
        set
        {
            _moneyAmountText = value;
            OnPropertyChanged();
        }
    }

    public string PowerValueText
    {
        get => _powerValueText;
        set
        {
            _powerValueText = value;
            OnPropertyChanged();
        }
    }

    public string ScPointValueText
    {
        get => _scPointValueText;
        set
        {
            _scPointValueText = value;
            OnPropertyChanged();
        }
    }

    public ReinforcementPreset? SelectedReinforcementPreset
    {
        get => _selectedReinforcementPreset;
        set
        {
            _selectedReinforcementPreset = value;
            OnPropertyChanged();
            RaiseCommandStates();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            _isBusy = value;
            OnPropertyChanged();
            RaiseCommandStates();
        }
    }

    public bool IsQueueRunning
    {
        get => _isQueueRunning;
        private set
        {
            _isQueueRunning = value;
            OnPropertyChanged();
            RaiseCommandStates();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public void RefreshProcess()
    {
        DisposeSession();
        var target = _locator.Find(_manifest.TargetProcess);
        if (target is null)
        {
            StatusMessage = "未找到 ra3_1.12。";
            RaiseCommandStates();
            return;
        }

        AttachTarget(target, autoInstall: true);
    }

    public async Task LaunchAndLoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            SaveLauncherSettings();
            StatusMessage = "正在启动 RA3.exe。";
            _launcher.Start(LauncherPath, LauncherArguments);

            StatusMessage = "已启动 RA3.exe，等待 ra3_1.12。";
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(CurrentSettings().AttachTimeoutSeconds + 5));
            var waiter = new GameProcessWaiter(_locator.Find);
            var target = await waiter.WaitForAsync(
                _manifest.TargetProcess,
                TimeSpan.FromSeconds(CurrentSettings().AttachTimeoutSeconds),
                cancellation.Token);
            if (target is null)
            {
                StatusMessage = "启动后未找到 ra3_1.12。";
                return;
            }

            AttachTarget(target, autoInstall: true);
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void SaveLauncherSettings()
    {
        try
        {
            _settingsStore.Save(CurrentSettings());
            StatusMessage = "设置已保存。";
        }
        catch (Exception ex)
        {
            StatusMessage = $"保存启动器路径失败：{ex.Message}";
        }
    }

    private void BrowseLauncherPath()
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择 RA3.exe",
                Filter = "RA3 launcher (RA3.exe)|RA3.exe|Executable files (*.exe)|*.exe|All files (*.*)|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                LauncherPath = dialog.FileName;
                SaveLauncherSettings();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"选择启动器路径失败：{ex.Message}";
        }
    }

    private void AttachTarget(TrainerTarget target, bool autoInstall)
    {
        try
        {
            if (target.ProcessId is null)
            {
                throw new InvalidOperationException("无法确定目标进程 PID。");
            }

            _targetProcessId = target.ProcessId;
            _memory = new Win32ProcessMemory(target.ProcessId.Value);
            _session = new TrainerSession(_manifest, _memory, target, _memory, bootstrapLines: TrainerRuntimeAssets.ReadBootstrapLines());
            var result = _session.Attach();
            StatusMessage = result.Message;
            if (result.Success && autoInstall)
            {
                InstallPatches();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            RaiseCommandStates();
        }
    }

    public void InstallPatches()
    {
        if (_session is null)
        {
            StatusMessage = "请先检测进程。";
            return;
        }

        try
        {
            var resourceValues = GetResourceValueSettings();
            _session.InstallPatches();
            if (_session.Resolver is not null && _memory is not null)
            {
                FeatureController = new FeatureController(_memory, _session.Resolver);
                FeatureController.WriteResourceValues(resourceValues);
            }
            ArePatchesInstalled = true;
            StartHotkeys();
            StatusMessage = $"Patch 已安装，快捷键已启用。Hook={_session.InstalledHookCount}；{_session.RemoteSymbolSummary}";
            ReadSelectedUnitCodeCommand.RaiseCanExecuteChanged();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    public void RestorePatches()
    {
        _session?.Dispose();
        FeatureController = null;
        ArePatchesInstalled = false;
        StopHotkeys();
        StatusMessage = "Patch 已恢复。";
        ReadSelectedUnitCodeCommand.RaiseCanExecuteChanged();
        RaiseCommandStates();
    }

    public ReinforcementSettings GetReinforcementSettings()
    {
        return ReinforcementSettings.Parse(
            ReinforcementUnitIdText,
            ReinforcementCountText,
            ReinforcementRankText);
    }

    public ResourceValueSettings GetResourceValueSettings()
    {
        return ResourceValueSettings.Parse(MoneyAmountText, PowerValueText, ScPointValueText);
    }

    public void WriteResourceValuesIfNeeded(TrainerFeature feature)
    {
        if (!IsResourceValueFeature(feature) || FeatureController is null)
        {
            return;
        }

        FeatureController.WriteResourceValues(GetResourceValueSettings());
    }

    public void ReadSelectedUnitCode()
    {
        if (FeatureController is null)
        {
            StatusMessage = "请先检测进程并安装 patch。";
            return;
        }

        try
        {
            var unitCode = FeatureController.ReadSelectedUnitCode();
            ReinforcementUnitIdText = $"0x{unitCode:X8}";
            StatusMessage = $"已读取选中单位代码：{ReinforcementUnitIdText}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"读取选中单位代码失败：{ex.Message}";
        }
    }

    public void OpenReinforcementUnitPicker()
    {
        try
        {
            var picker = new ReinforcementUnitPickerViewModel(_reinforcementUnits);
            var window = new ReinforcementUnitPickerWindow
            {
                Owner = Application.Current?.MainWindow,
                DataContext = picker
            };

            var result = window.ShowDialog();
            _reinforcementUnits = picker.Units.ToArray();

            if (result == true && picker.SelectedUnit is not null)
            {
                ReinforcementUnitIdText = picker.SelectedUnit.CodeText;
                StatusMessage = $"已选择单位：{picker.SelectedUnit.Name} ({picker.SelectedUnit.CodeText})";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"打开单位列表失败：{ex.Message}";
        }
    }

    private void SaveReinforcementPreset()
    {
        try
        {
            var settings = GetReinforcementSettings();
            var preset = new ReinforcementPreset(PresetNameText, settings.UnitId, settings.Count, settings.Rank);
            var existingIndex = IndexOfPreset(preset.Name);
            if (existingIndex >= 0)
            {
                ReinforcementPresets[existingIndex] = preset;
            }
            else
            {
                ReinforcementPresets.Add(preset);
            }

            SelectedReinforcementPreset = preset;
            PersistSettings();
            StatusMessage = $"已保存增援预设：{preset.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"保存增援预设失败：{ex.Message}";
        }
    }

    private void ApplySelectedReinforcementPreset()
    {
        if (SelectedReinforcementPreset is null)
        {
            return;
        }

        ApplyPreset(SelectedReinforcementPreset);
        StatusMessage = $"已应用增援预设：{SelectedReinforcementPreset.Name}";
    }

    private void AddSelectedPresetToQueue()
    {
        if (SelectedReinforcementPreset is null)
        {
            return;
        }

        AddQueueEntry(SelectedReinforcementPreset.ToQueueEntry());
    }

    private void AddCurrentReinforcementToQueue()
    {
        var name = string.IsNullOrWhiteSpace(PresetNameText) ? ReinforcementUnitIdText : PresetNameText;
        AddQueueEntry(new ReinforcementQueueEntry(name, ReinforcementUnitIdText, ReinforcementCountText, ReinforcementRankText));
    }

    private async Task ExecuteReinforcementQueueAsync()
    {
        if (FeatureController is null)
        {
            StatusMessage = "请先检测进程并安装 patch。";
            return;
        }

        var feature = FindUiFeature("We Need Back");
        if (feature is null)
        {
            StatusMessage = "找不到增援功能。";
            return;
        }

        IsQueueRunning = true;
        try
        {
            var executed = 0;
            var skipped = 0;
            foreach (var item in ReinforcementQueue)
            {
                item.Status = "执行中";
                item.Message = string.Empty;
                var results = await ReinforcementQueueRunner.ExecuteAsync(
                    [item.ToEntry()],
                    FeatureController,
                    feature,
                    FeatureController.DefaultDispatchTimeout,
                    FeatureController.DefaultDispatchPollInterval);
                var result = results[0];
                item.ApplyResult(result);
                if (result.Status == ReinforcementQueueItemStatus.Executed)
                {
                    executed++;
                }
                else if (result.Status == ReinforcementQueueItemStatus.Skipped)
                {
                    skipped++;
                }
            }

            StatusMessage = $"增援队列执行完成：成功 {executed}，跳过 {skipped}。";
        }
        catch (Exception ex)
        {
            StatusMessage = $"增援队列执行失败：{ex.Message}";
        }
        finally
        {
            IsQueueRunning = false;
        }
    }

    private void ClearReinforcementQueue()
    {
        ReinforcementQueue.Clear();
        StatusMessage = "增援队列已清空。";
    }

    public void Dispose()
    {
        DisposeSession();
    }

    private void DisposeSession()
    {
        FeatureController = null;
        ArePatchesInstalled = false;
        StopHotkeys();
        _session?.Dispose();
        _session = null;
        _memory?.Dispose();
        _memory = null;
        _targetProcessId = null;
    }

    private void RaiseCommandStates()
    {
        BrowseLauncherCommand.RaiseCanExecuteChanged();
        LaunchAndLoadCommand.RaiseCanExecuteChanged();
        OpenReinforcementUnitPickerCommand.RaiseCanExecuteChanged();
        SaveReinforcementPresetCommand.RaiseCanExecuteChanged();
        ApplyReinforcementPresetCommand.RaiseCanExecuteChanged();
        AddSelectedPresetToQueueCommand.RaiseCanExecuteChanged();
        AddCurrentReinforcementToQueueCommand.RaiseCanExecuteChanged();
        ExecuteReinforcementQueueCommand.RaiseCanExecuteChanged();
        ClearReinforcementQueueCommand.RaiseCanExecuteChanged();
        InstallPatchesCommand.RaiseCanExecuteChanged();
        RestorePatchesCommand.RaiseCanExecuteChanged();
        ReadSelectedUnitCodeCommand.RaiseCanExecuteChanged();
        foreach (var item in ReinforcementQueue)
        {
            item.RaiseCommandState();
        }
    }

    private void RaiseFeatureCommandStates()
    {
        foreach (var item in Groups.SelectMany(group => group.Features))
        {
            item.RaiseCommandState();
        }
    }

    private IReadOnlyList<FeatureGroupViewModel> CreateGroups(IEnumerable<TrainerFeature> features)
    {
        var items = features.Select(feature => new FeatureItemViewModel(feature, this)).ToArray();
        return new[]
        {
            Group("玩家资源", items, "增加玩家战场资金", "无限电力", "无限秘密协议点数", "解开所有秘密协议技能"),
            Group("建造与地图", items, "快速建造建筑物/单位", "消散战争迷雾", "无限缩放", "禁止电脑建造建筑物/单位", "建筑物可随地建造"),
            Group("超级武器", items, "秘密协议技能与超级武器快速冷却", "禁止使用技能"),
            Group("单位操作", items, "选择的单位快速升级", "选择的单位高速移动", "选择的单位缓慢移动", "选择的单位暂停", "选择的单位恢复速度", "选择的建筑物/单位无限生命值", "选择的建筑物/单位生命值变为1", "选择的建筑物/单位恢复原本的生命值", "选择的单位无限弹药/炸弹", "选择的建筑物/单位ID", "摧毁选择的建筑物/单位"),
            Group("危险等级与矿点", items, "威胁等级最大", "威胁等级最高", "威胁等级恢复原状", "选择的矿脉恢复采集矿量"),
            Group("生成与复制", items, "给玩家基地车", "复制选择的建筑物/单位给玩家", "呼叫战场增援")
        };
    }

    private static FeatureGroupViewModel Group(string name, IReadOnlyList<FeatureItemViewModel> items, params string[] names)
    {
        var selected = names
            .Select(displayName => items.FirstOrDefault(item => item.DisplayName == displayName))
            .Where(item => item is not null)
            .Cast<FeatureItemViewModel>()
            .ToArray();
        return new FeatureGroupViewModel(name, selected);
    }

    private TrainerAppSettings CurrentSettings()
    {
        return new TrainerAppSettings(
            (LauncherPath ?? string.Empty).Trim(),
            (LauncherArguments ?? string.Empty).Trim(),
            _attachTimeoutSeconds,
            GetResourceValueSettings(),
            ReinforcementPresets.ToArray());
    }

    private void PersistSettings()
    {
        _settingsStore.Save(CurrentSettings());
    }

    private void ApplyPreset(ReinforcementPreset preset)
    {
        PresetNameText = preset.Name;
        ReinforcementUnitIdText = $"0x{preset.UnitId:X8}";
        ReinforcementCountText = preset.Count.ToString();
        ReinforcementRankText = preset.Rank.ToString();
    }

    private void AddQueueEntry(ReinforcementQueueEntry entry)
    {
        ReinforcementQueue.Add(new ReinforcementQueueItemViewModel(
            entry.Name,
            entry.UnitIdText,
            entry.CountText,
            entry.RankText,
            RemoveQueueItem,
            () => !IsQueueRunning));
        StatusMessage = $"已加入增援队列：{entry.Name}";
    }

    private void RemoveQueueItem(ReinforcementQueueItemViewModel item)
    {
        ReinforcementQueue.Remove(item);
    }

    private int IndexOfPreset(string name)
    {
        for (var index = 0; index < ReinforcementPresets.Count; index++)
        {
            if (ReinforcementPresets[index].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private TrainerFeature? FindUiFeature(string rawName)
    {
        return Groups
            .SelectMany(group => group.Features)
            .Select(item => item.Feature)
            .FirstOrDefault(feature => feature.RawName.Equals(rawName, StringComparison.Ordinal));
    }

    private static bool IsResourceValueFeature(TrainerFeature feature)
    {
        return feature.RawName is "Moeny" or "Power" or "SC POINT";
    }

    private void StartHotkeys()
    {
        var bindings = Groups
            .SelectMany(group => group.Features)
            .Select(item => HotkeyGesture.TryParse(item.Hotkey, out var gesture)
                ? new HotkeyFeatureBinding(gesture, item.Feature, item.ExecuteFromHotkey)
                : null)
            .Where(binding => binding is not null)
            .Cast<HotkeyFeatureBinding>()
            .ToArray();

        _hotkeyDispatcher.Update(bindings, enabled: true);
        _keyboardHook.Install();
    }

    private void StopHotkeys()
    {
        _hotkeyDispatcher.Update([], enabled: false);
        _keyboardHook.Uninstall();
    }

    private void OnKeyboardHookKeyDown(object? sender, KeyboardHookEventArgs e)
    {
        if (!IsTargetGameForeground())
        {
            return;
        }

        App.Current.Dispatcher.Invoke(() =>
        {
            e.Handled = _hotkeyDispatcher.TryDispatch(e.VirtualKey, e.Modifiers);
        });
    }

    private void OnKeyboardHookKeyUp(object? sender, int virtualKey)
    {
        _hotkeyDispatcher.Release(virtualKey);
    }

    private bool IsTargetGameForeground()
    {
        return _targetProcessId is int targetProcessId &&
            _foregroundWindowProcess.GetForegroundProcessId() == targetProcessId;
    }
}
