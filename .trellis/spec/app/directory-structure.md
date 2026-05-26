# 目录结构（App）

> Ra3Trainer.App 项目的文件组织与命名约定。

---

## 目录布局

```
src/Ra3Trainer.App/
├── Views/
│   ├── ReinforcementUnitPickerWindow.xaml  # 增援单位选择器
│   └── ReinforcementUnitPickerWindow.xaml.cs
├── ViewModels/
│   ├── ViewModelBase.cs             # MVVM 基类（INotifyPropertyChanged）
│   ├── RelayCommand.cs              # ICommand 实现
│   ├── MainViewModel.cs             # 主 ViewModel（单一大 VM 协调器）
│   ├── FeatureGroupViewModel.cs     # 功能分组 VM
│   ├── FeatureItemViewModel.cs      # 单个功能项 VM
│   ├── ReinforcementQueueItemViewModel.cs  # 增援队列项 VM
│   └── ReinforcementUnitPickerViewModel.cs # 单位选择器 VM
├── Hotkeys/
│   ├── LowLevelKeyboardHook.cs      # WH_KEYBOARD_LL 全局钩子
│   ├── KeyboardHookEventArgs.cs     # 钩子事件参数
│   └── ForegroundWindowProcess.cs   # 前台窗口进程 ID 检测
├── App.xaml                         # 应用程序入口（资源/样式）
├── App.xaml.cs                      # 启动逻辑
├── MainWindow.xaml                  # 主窗口
└── Ra3Trainer.App.csproj            # 项目文件（win-x86, UseWPF）
```

---

## 各层职责

### Views — XAML + Code-Behind

**职责：** 声明式 UI 布局。code-behind 只做两件事：`InitializeComponent()` 和 `DataContext` 赋值。**绝不含**业务逻辑、数据处理或游戏操作。

```csharp
// MainWindow.xaml.cs — 典型的 code-behind
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel vm)  // 可选：构造函数注入
    {
        InitializeComponent();
        DataContext = vm;
    }
}
```

### ViewModels — MVVM 层

**职责：** 暴露可绑定属性和命令，调度 Core 层功能。

**特征：**
- 单一 `MainViewModel` 作为协调器，持有所有 `FeatureItemViewModel`、`ReinforcementQueueItemViewModel`
- 不拆分 `UserControl` 级别的 VM——本项目规模下，一级 VM 足够
- 禁止在 VM 中引用 `System.Windows` 命名空间中的类型（`MessageBox`、`Window`、`Clipboard` 等）

**代码示例：** `src/Ra3Trainer.App/ViewModels/MainViewModel.cs`
```csharp
public sealed class MainViewModel : ViewModelBase, IDisposable
{
    public ObservableCollection<FeatureGroupViewModel> Groups { get; }
    public ObservableCollection<ReinforcementQueueItemViewModel> ReinforcementQueue { get; }

    // 所有子 VM 由 MainViewModel 统一持有并协调
    public RelayCommand RefreshCommand { get; }
    public RelayCommand InstallPatchesCommand { get; }
}
```

### Hotkeys — 全局键盘钩子

**职责：** Win32 `SetWindowsHookEx(WH_KEYBOARD_LL)` 的低级封装。独立于 WPF 消息循环。

**特征：**
- `LowLevelKeyboardHook` 安装全局钩子，`KeyDown`/`KeyUp` 事件通知
- `ForegroundWindowProcess` 检测前台窗口是否为目标游戏进程（只在游戏窗口激活时响应热键）
- 热键生命周期：`Install()` → 事件分发 → `Uninstall()`

---

## 命名约定

| 规则 | 示例 |
|------|------|
| View 文件以 `Window` 结尾 | `MainWindow.xaml`, `ReinforcementUnitPickerWindow.xaml` |
| ViewModel 文件以 `ViewModel` 结尾 | `MainViewModel.cs`, `FeatureItemViewModel.cs` |
| XAML 控件名默认不设 `x:Name` | 依赖 Binding，不需要后台代码引用 |
| RelayCommand 属性以 `Command` 结尾 | `LaunchAndLoadCommand`, `InstallPatchesCommand` |

---

## 禁止模式

1. **禁止在 code-behind 中写业务逻辑。** `.xaml.cs` 文件超过 10 行视为设计问题。
2. **禁止在 ViewModel 中 new Window 或 MessageBox。** 所有 UI 交互通过 Binding + Command。
3. **禁止绕过 ViewModel 直接操作 Core 层。** App 层只通过 `MainViewModel` 访问 Core。
4. **禁止在 XAML 中使用内联 C# 代码（`x:Code`）。**
