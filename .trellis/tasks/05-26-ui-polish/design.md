# Technical Design: UI Polish

## Architecture Overview

```
Ra3Trainer.App/
├── ViewModels/
│   ├── MainViewModel.cs              # ← 加 ConnectionState 枚举+属性
│   └── FeatureItemViewModel.cs       # ← 加 IsActive 计算属性
├── Converters/
│   └── ConnectionStateToBrushConverter.cs  # ← 新建
├── Themes/
│   ├── GlobalStyles.xaml             # ← 加 DangerButtonStyle + PrimaryButtonStyle DataTrigger
│   └── SemanticTokens.xaml           # ← 改 InputBackgroundBrush 值
├── MainWindow.xaml                   # ← 布局重组 + 按钮 Style 分配 + 状态圆点
└── Views/
    └── ReinforcementUnitPickerWindow.xaml  # 不改动
```

## 1. ConnectionState Enum

```csharp
public enum ConnectionState
{
    Disconnected,  // 未检测/未连接
    Connected,     // 已连接 + patch 已安装
    Processing     // 正在启动/检测中
}
```

`MainViewModel` 加属性：
```csharp
public ConnectionState ConnectionState { get; private set; }
```

设置时机：
- `RefreshProcess()` 开始 → Processing
- 未找到进程 → Disconnected
- Patch 安装成功 → Connected
- Patch 恢复 → Disconnected
- 启动游戏 → Processing

状态文本 `StatusMessage` 保持不变。`ConnectionState` 仅驱动颜色圆点。

## 2. FeatureItemViewModel.IsActive

```csharp
public bool IsActive => _enabled;
```

无需 setter——`_enabled` 已通过 `ExecuteAsync()` 更新，`IsActive` 通过 `OnPropertyChanged(nameof(IsActive))` 通知（在 `_enabled` 变更时）。

## 3. Converters

### ConnectionStateToBrushConverter : IValueConverter

```csharp
// Disconnected → ErrorBrush
// Connected    → SuccessBrush
// Processing   → WarningBrush
```

## 4. Button Style Changes

### DangerButtonStyle (新增, x:Key)

完全复制默认 Button 模板，只改 hover trigger：

```xml
<Trigger Property="IsMouseOver" Value="True">
    <Setter TargetName="border" Property="BorderBrush" Value="{StaticResource ErrorBrush}" />
    <Setter TargetName="border" Property="Background" Value="{StaticResource ErrorBrush}" />
    <!-- 文字改为 Base (深色) ← 因为 ErrorBrush 背景上白字对比度差 -->
</Trigger>
```

### PrimaryButtonStyle DataTrigger (修改)

在已有 ControlTemplate.Triggers 中追加：

```xml
<DataTrigger Binding="{Binding IsActive}" Value="True">
    <Setter TargetName="border" Property="Background" Value="{StaticResource SuccessBrush}" />
    <Setter TargetName="border" Property="BorderBrush" Value="{StaticResource SuccessBrush}" />
</DataTrigger>
```

注意：DataTrigger 优先级低于 IsMouseOver/IsPressed trigger → hover/press 仍生效。

## 5. Semantic Token Change

`InputBackgroundBrush`: `Surface0` → `Base` (#1E1E2E)

效果：输入框在 Surface0 卡片上形成视觉凹陷（Base 比 Surface0 更深）。

## 6. Layout Restructure

### Top Bar (精简后)

仅保留：
- 标题 "RA3 Trainer" + 状态（含颜色圆点）
- 路径 + 参数 + 浏览/保存/一键启动/安装Patch/恢复Patch
- 移除：Money/Power/SC/增援ID/数量/星级

### "基础资源" Card (新建)

```xml
<Border Background="{StaticResource CardBackgroundBrush}"
        BorderBrush="{StaticResource CardBorderBrush}"
        BorderThickness="1" CornerRadius="6"
        Margin="0,0,0,14" Padding="14">
  <StackPanel>
    <TextBlock Text="基础资源" FontSize="16" FontWeight="SemiBold"
               Foreground="{StaticResource TextPrimaryBrush}" Margin="0,0,0,10" />
    <Grid>
      <!-- Money / Power / SC Point 三列 -->
    </Grid>
  </StackPanel>
</Border>
```

位置：ScrollViewer 内第一个元素。

### "增援预设/队列" Card (调整)

- 10 个按钮分两组：预设管理（保存/应用/预设入队/当前入队）+ 队列操作（执行/清空）
- 按钮间距 `Margin="12,0,0,0"`（当前 8px）

### Feature Items (调整)

- `Hotkey` TextBlock Foreground 从 `TextTertiaryBrush` 改为 `TextDisabledBrush`

## 7. Button Style Assignment Map

| 按钮 | Style |
|------|-------|
| "重新检测进程" | 默认 (implicit) |
| "安装 Patch" | 默认 |
| "恢复所有 Patch" | `DangerButtonStyle` |
| "浏览" | 默认 |
| "保存设置" | 默认 |
| "一键启动并装载" | `PrimaryButtonStyle` |
| "保存预设" | 默认 |
| "应用预设" | 默认 |
| "预设入队" | 默认 |
| "当前入队" | 默认 |
| "执行队列" | `PrimaryButtonStyle` |
| "清空队列" | `DangerButtonStyle` |
| "单位列表" | 默认 |
| "读取选中单位" | 默认 |
| Feature "开启/执行" | `PrimaryButtonStyle` |
| Queue item "移除" | `DangerButtonStyle` |
| Picker "确认" | 默认 |
| Picker "取消" | 默认 |
| Picker "导入文件" | 默认 |

## 8. Files NOT Changed

- `Ra3Trainer.Core` (any file)
- `tests/` (any file)
- `App.xaml`
- `App.xaml.cs`
- All code-behind files
- `CatppuccinMocha.xaml`
- `ReinforcementUnitPickerWindow.xaml` (already themed, no button semantic changes needed)
