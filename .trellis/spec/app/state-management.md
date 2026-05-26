# 状态管理 — MVVM 模式

> Ra3Trainer.App 使用自建 MVVM 基础设施（非 CommunityToolkit），单一大 ViewModel 协调器模式。

---

## ViewModelBase — INotifyPropertyChanged 基础

**位置：** `src/Ra3Trainer.App/ViewModels/ViewModelBase.cs`

```csharp
public class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T backingStore, T value,
        [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;

        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
```

**设计说明：**
- 没有引入 `CommunityToolkit.Mvvm`（避第三方依赖）——自建基类约 20 行
- `SetProperty<T>` 自带变更检测——值相同不触发通知
- `[CallerMemberName]` 消除魔术字符串

### 使用示例

**位置：** `src/Ra3Trainer.App/ViewModels/MainViewModel.cs:130-138`

```csharp
public string LauncherPath
{
    get => _launcherPath;
    set
    {
        _launcherPath = value;
        OnPropertyChanged();  // CallerMemberName 自动填入 "LauncherPath"
    }
}
```

### 批量刷新命令状态

多个属性变更后需刷新所有 `CanExecute`：

**位置：** `src/Ra3Trainer.App/ViewModels/MainViewModel.cs:619-638`

```csharp
private void RaiseCommandStates()
{
    BrowseLauncherCommand.RaiseCanExecuteChanged();
    LaunchAndLoadCommand.RaiseCanExecuteChanged();
    InstallPatchesCommand.RaiseCanExecuteChanged();
    RestorePatchesCommand.RaiseCanExecuteChanged();
    // ... 所有命令
    foreach (var item in ReinforcementQueue)
        item.RaiseCommandState();
}

private void RaiseFeatureCommandStates()
{
    foreach (var item in Groups.SelectMany(group => group.Features))
        item.RaiseCommandState();
}
```

**原则：** 手动批量通知——因为项目只有 15 个命令，不需要自动化命令刷新框架。

---

## RelayCommand — ICommand 实现

**位置：** `src/Ra3Trainer.App/ViewModels/RelayCommand.cs`

```csharp
public class RelayCommand : ICommand
{
    private readonly Action<object> _execute;
    private readonly Func<object, bool> _canExecute;

    public RelayCommand(Action execute, Func<bool> canExecute = null)
        : this(_ => execute(), canExecute is null ? null : _ => canExecute()) { }

    public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object parameter) => _execute(parameter);

    public event EventHandler CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
```

**设计说明：**
- 两个构造函数重载——无参数版本（最常用）+ 泛型 object 版本
- 使用 `CommandManager.RequerySuggested` 自动触发 `CanExecuteChanged`——绑定到按钮的 `IsEnabled` 自动更新
- 手动调用 `RaiseCanExecuteChanged()` 用于立即刷新

### 命令创建模式

**位置：** `src/Ra3Trainer.App/ViewModels/MainViewModel.cs:59-73`

```csharp
// 无条件命令
RefreshCommand = new RelayCommand(RefreshProcess);

// 带 CanExecute 的命令
LaunchAndLoadCommand = new RelayCommand(() => _ = LaunchAndLoadAsync(), () => !IsBusy);

// 复杂 CanExecute（在 RaiseCommandStates 中统一刷新）
InstallPatchesCommand = new RelayCommand(InstallPatches,
    () => _session?.CanUseFeatures == true && !ArePatchesInstalled);
```

---

## ObservableCollection — 列表绑定

**位置：** `src/Ra3Trainer.App/ViewModels/MainViewModel.cs:56-58`

```csharp
ReinforcementPresets = new ObservableCollection<ReinforcementPreset>(settings.ReinforcementPresets);
ReinforcementQueue = new ObservableCollection<ReinforcementQueueItemViewModel>();
// CollectionChanged → 自动刷新 CanExecute
ReinforcementQueue.CollectionChanged += (_, _) => RaiseCommandStates();
```

**规则：**
- 所有绑定到 `ItemsControl.ItemsSource` 的集合必须是 `ObservableCollection<T>`
- 不替换整个集合——用 `Clear()` + `Add()` 操作
- 集合变更时手动刷新相关命令状态

---

## 单一大 ViewModel 协调器模式

本项目的 MVVM 不走"每个 Window 一个独立 VM"的严格模式。`MainViewModel` 作为唯一协调器：

```
MainViewModel (782 行)
├── Groups: ObservableCollection<FeatureGroupViewModel>
│   └── Features: FeatureItemViewModel[]
├── ReinforcementQueue: ObservableCollection<ReinforcementQueueItemViewModel>
├── ReinforcementPresets: ObservableCollection<ReinforcementPreset>
└── 15 个 RelayCommand 属性
```

**何时拆分子 ViewModel：**
- 有独立 Window（如 `ReinforcementUnitPickerWindow`）→ 独立 VM（`ReinforcementUnitPickerViewModel`）
- 所有在主窗口内的 → 作为 `MainViewModel` 的子集合，不需要独立 VM

**跨 VM 通信：** 不引入 Messenger。`MainViewModel` 直接持有子 VM 引用并协调。`ReinforcementUnitPickerWindow` 通过 `ShowDialog` 同步返回结果。

---

## 禁止模式

1. **禁止在 ViewModel 中使用 `Dispatcher.Invoke` 操作 UI。** 用 Binding 自动同步，WPF 绑定引擎内置线程安全。
2. **禁止用 `List<T>` 替换 `ObservableCollection<T>` 实例。** 始终用 `.Clear()` + `.Add()`。
3. **禁止在属性 getter 中执行重逻辑。** 属性 getter 可被多次调用（绑定引擎可能重新求值）。
4. **禁止在 ViewModel 构造函数中执行异步操作。** 构造函数同步完成，异步初始化放到 `Loaded` 事件或 `async void` 命令中。
5. **禁止跨 VM 直接修改对方属性。** 通过 MainViewModel 协调，或通过方法调用而非属性赋值。
