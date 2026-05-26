# 质量规范（App）

> Ra3Trainer.App 的 XAML 规范、ViewModel 可测试性、禁止模式与测试标准。

---

## XAML 规范

### 命名空间组织

```xml
<Window x:Class="Ra3Trainer.App.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:Ra3Trainer.App.ViewModels"
        Title="RA3 Trainer" Height="720" Width="1080">
```

**规则：**
- 不引入多余 xmlns 别名——只声明实际用到的
- `vm:` 别名指向 ViewModels namespace（用于 DataTemplate 类型匹配）

### 控件属性顺序

按以下顺序排列属性：
1. 布局（`Grid.Row`, `Grid.Column`, `DockPanel.Dock`）
2. 内容（`Text`, `Content`, `Header`）
3. 绑定（`Command`, `ItemsSource`, `Text="{Binding ...}"`）
4. 外观（`Padding`, `Margin`, `Foreground`, `Background`）
5. 尺寸（`Width`, `Height`, `MinWidth`, `MinHeight`）

### 避免内联样式

```xml
<!-- ✅ 用固定颜色常量（工业级中性色） -->
Background="#1F2937"
Foreground="#CBD5E1"

<!-- ❌ 不引入 Style 资源字典——项目规模不需要 -->
```

**原则：** 颜色直接写 XAML 属性值。不创建 Style/ResourceDictionary——项目有 1 个窗口，不需要主题系统。

---

## Code-Behind 最小原则

`MainWindow.xaml.cs` 的标准形态：

```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = MainViewModel.LoadDefault();
    }
}
```

**检查清单：**
- [ ] `.xaml.cs` 文件 ≤ 15 行
- [ ] 不含事件处理器（Click、SelectionChanged 等）——用 Command 绑定
- [ ] 不含业务逻辑或数据处理
- [ ] 不含 `MessageBox.Show()` —— 状态通过 `StatusMessage` 属性显示
- [ ] 构造函数只做：`InitializeComponent()` + `DataContext` 赋值

---

## ViewModel 可测试性

### 依赖注入（手动，非 DI 容器）

```csharp
// MainViewModel 通过构造函数接受依赖
private MainViewModel(TrainerManifest manifest, TrainerAppSettingsStore settingsStore)
{
    _manifest = manifest;
    _settingsStore = settingsStore;
    // ...
}

// 静态工厂方法用于生产代码
public static MainViewModel LoadDefault()
{
    return new MainViewModel(TrainerRuntimeAssets.LoadManifest(), new TrainerAppSettingsStore());
}
```

**设计说明：**
- 生产代码调用 `LoadDefault()` 自动装配
- 测试代码可调用 `new MainViewModel(fakeManifest, fakeStore)` 注入 fake
- 避免引入 DI 容器——项目规模不需要

### 状态属性独立可测

每个属性变更通过 `OnPropertyChanged` / `SetProperty` 通知，可通过事件订阅验证：

```csharp
// 测试 ArePatchesInstalled 属性变更
var receivedEvents = new List<string>();
vm.PropertyChanged += (_, e) => receivedEvents.Add(e.PropertyName);
vm.InstallPatches();
Assert.Contains("ArePatchesInstalled", receivedEvents);
```

### 命令可单独调用

```csharp
// 直接调用命令的 Execute 方法测试行为
vm.InstallPatchesCommand.Execute(null);
Assert.True(vm.ArePatchesInstalled);
```

---

## 测试命名

**位置：** `tests/Ra3Trainer.Tests/`

现有测试示例：

| 测试文件 | 示例方法名 |
|---------|-----------|
| `ReinforcementUnitPickerViewModelTests` | `SelectedUnit_ShouldBeNull_WhenFilterReturnsEmpty` |
| `ReinforcementSettingsTests` | `Parse_ShouldReturnDefault_WhenInputsAreInvalid` |
| `TrainerAppSettingsStoreTests` | `Load_ShouldReturnDefault_WhenFileDoesNotExist` |

**模式：** `MethodOrScenario_ExpectedResult_WhenCondition`

---

## 禁止模式

1. **禁止在 ViewModel 中引用 `System.Windows` 命名空间**（`MessageBox`, `Window`, `Clipboard`, `Application.Current` 等）。违反此规则 = 不可测试。
2. **禁止 code-behind 处理业务异常。** 异常在 Core 层捕获，App 层只显示消息。
3. **禁止在 ViewModel 属性 getter 中调用 Core 层读取方法。** 属性 getter = 被动取值。主动读取用命令。
4. **禁止 `Dispatcher.BeginInvoke` 做"后台转前台"。** 绑定引擎已处理线程同步。ViewModel 属性更新可来自任何线程。
5. **禁止硬编码中文字符串分布在多个文件。** `TrainerFeatureCatalog.SourceTrainerOverrides` 字典是唯一的中文映射中心。
6. **禁止在测试中依赖文件系统。** 用 `FakeProcessMemory` 等 fake，不读写实际文件。
