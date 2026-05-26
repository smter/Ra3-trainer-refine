# 组件规范 — WPF Window/XAML

> WPF 窗口与 XAML 组件的设计模式、绑定约定与 code-behind 原则。

---

## Window 结构模式

### XAML 布局

**位置：** `src/Ra3Trainer.App/MainWindow.xaml:1-50`

```xml
<Window x:Class="Ra3Trainer.App.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="RA3 Trainer" Height="720" Width="1080"
        MinHeight="560" MinWidth="860">
  <DockPanel>
    <!-- 顶部工具栏：暗色背景 -->
    <Border DockPanel.Dock="Top" Background="{StaticResource TopBarBackgroundBrush}" Padding="16">
      <StackPanel>
        <TextBlock Text="RA3 Trainer" FontSize="22" FontWeight="SemiBold"
                   Foreground="{StaticResource TextPrimaryBrush}" />
        <TextBlock Text="{Binding StatusMessage}"
                   Foreground="{StaticResource TextSecondaryBrush}" />
      </StackPanel>
    </Border>
    <!-- 主体内容 -->
  </DockPanel>
</Window>
```

**原则：**
- 窗口尺寸设 `MinHeight`/`MinWidth` 防止布局压碎
- 颜色使用 Catppuccin Mocha 暗色主题，通过 `{StaticResource}` 引用语义化 token（参见 `Themes/` 目录）
- `DockPanel` 作为顶层布局（顶部固定 + 主体滚动）

### Code-Behind 最小原则

**位置：** `src/Ra3Trainer.App/MainWindow.xaml.cs`

```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = MainViewModel.LoadDefault();  // 静态工厂方法
    }
}
```

**规则：**
- code-behind **不超过此模式**——不添加事件处理器、不写数据操作
- 若需要 View 特定逻辑（如拖拽、动画），优先用 WPF Behavior/Attached Property，而不是 code-behind

---

## XAML 绑定模式

### 数据绑定

```xml
<!-- 单向绑定（只读状态） -->
<TextBlock Text="{Binding StatusMessage}" />

<!-- 双向绑定（用户输入） -->
<TextBox Text="{Binding LauncherPath, UpdateSourceTrigger=PropertyChanged}" />

<!-- 集合绑定 -->
<ListBox ItemsSource="{Binding ReinforcementQueue}"
         DisplayMemberPath="Name" />
```

### 命令绑定

```xml
<Button Content="检测进程" Command="{Binding RefreshCommand}" Padding="12,8" />

<!-- 带 CanExecute 的命令会自动禁用按钮 -->
<Button Content="安装 Patch" Command="{Binding InstallPatchesCommand}" />
```

**位置：** `src/Ra3Trainer.App/ViewModels/MainViewModel.cs:59-73`

```csharp
// 命令定义（在构造函数中）
RefreshCommand = new RelayCommand(RefreshProcess);
LaunchAndLoadCommand = new RelayCommand(() => _ = LaunchAndLoadAsync(), () => !IsBusy);
InstallPatchesCommand = new RelayCommand(InstallPatches, () => _session?.CanUseFeatures == true && !ArePatchesInstalled);
```

### DataTemplate 分组显示

功能卡片按分组排列，每个 `FeatureGroupViewModel` 含一组 `FeatureItemViewModel`：

```xml
<ItemsControl ItemsSource="{Binding Groups}">
  <ItemsControl.ItemTemplate>
    <DataTemplate DataType="{x:Type vm:FeatureGroupViewModel}">
      <GroupBox Header="{Binding Name}">
        <ItemsControl ItemsSource="{Binding Features}" />
      </GroupBox>
    </DataTemplate>
  </ItemsControl.ItemTemplate>
</ItemsControl>
```

---

## 模式：窗口作为对话框

用于单位选择器等辅助窗口。

**位置：** `src/Ra3Trainer.App/ViewModels/MainViewModel.cs:463-487`

```csharp
public void OpenReinforcementUnitPicker()
{
    var picker = new ReinforcementUnitPickerViewModel(_reinforcementUnits);
    var window = new ReinforcementUnitPickerWindow
    {
        Owner = Application.Current?.MainWindow,  // 模态于主窗口之上
        DataContext = picker
    };

    var result = window.ShowDialog();
    // 关闭对话框后才读取选择结果
    if (result == true && picker.SelectedUnit is not null)
    {
        ReinforcementUnitIdText = picker.SelectedUnit.CodeText;
    }
}
```

**原则：**
- 对话框窗口设 `Owner` 确保正确 Z-order
- `ShowDialog()` 返回后读取 VM 状态——不在窗口关闭事件中操作
- 对话框 VM 是临时对象，不需要长生命周期

---

## 禁止模式

1. **禁止在 code-behind 中写 Click 事件处理器。** 所有按钮用 `Command` 绑定。
2. **禁止在 XAML 中硬编码数据。** 所有内容来自 Binding。
3. **禁止 ViewModel 持有对 View 的引用。** VM 不知道（也不应该知道）自己由哪个 Window 承载。
4. **禁止在 XAML DataTemplate 中嵌套复杂逻辑。** 用 `IValueConverter` 或 ViewModel 属性处理转换。
5. **禁止使用 `DynamicResource` 做主题切换。** 项目使用 Catppuccin Mocha 暗色主题，颜色引用统一用 `StaticResource`。不引入 DynamicResource 做运行时换肤。
