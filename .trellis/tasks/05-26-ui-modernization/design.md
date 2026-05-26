# Technical Design: UI Modernization

## Architecture Overview

```
Ra3Trainer.App/
├── App.xaml                          # ← 修改: 合并主题资源字典
├── Themes/
│   ├── CatppuccinMocha.xaml          # ← 新建: 25 色 Catppuccin 颜色定义
│   ├── SemanticTokens.xaml           # ← 新建: 语义化 Brush 映射
│   ├── GlobalStyles.xaml             # ← 新建: 隐式控件 Style
│   └── SarasaMonoSC-Regular.ttf      # ← 新建: 字体文件 (BuildAction=Resource)
├── MainWindow.xaml                   # ← 修改: 硬编码颜色 → {StaticResource}
├── MainWindow.xaml.cs                # 不改动
├── Views/
│   └── ReinforcementUnitPickerWindow.xaml  # ← 修改: 硬编码颜色 → {StaticResource}
└── ViewModels/                       # 完全不改动
```

**原则**: 仅修改 XAML 视觉层。ViewModel、Core 项目、code-behind 零改动。

## Color System: Catppuccin Mocha

### Layer 1: Raw Color Palette (`CatppuccinMocha.xaml`)

25 个 `Color` 资源，Key 命名 `Catppuccin.{Name}`。示例：

```xml
<Color x:Key="Catppuccin.Base">#1e1e2e</Color>
<Color x:Key="Catppuccin.Surface0">#313244</Color>
<Color x:Key="Catppuccin.Text">#cdd6f4</Color>
<Color x:Key="Catppuccin.Mauve">#cba6f7</Color>
```

### Layer 2: Semantic Tokens (`SemanticTokens.xaml`)

将原始色映射到 UI 语义角色，使用 `SolidColorBrush`：

| Token | Source | Usage |
|-------|--------|-------|
| `WindowBackgroundBrush` | Base | 窗口背景 |
| `TopBarBackgroundBrush` | Mantle | 顶栏背景 |
| `CardBackgroundBrush` | Surface0 | 卡片/面板背景 |
| `CardBorderBrush` | Overlay0 | 卡片边框 |
| `ElevatedSurfaceBrush` | Surface1 | 弹出层/下拉框 |
| `TextPrimaryBrush` | Text | 正文 |
| `TextSecondaryBrush` | Subtext1 | 标签、说明、状态 |
| `TextDisabledBrush` | Overlay2 | 禁用状态文字 |
| `PrimaryAccentBrush` | Mauve | 主按钮/强调色 |
| `SecondaryAccentBrush` | Lavender | 辅助强调/悬停高亮 |
| `InfoAccentBrush` | Blue | 信息提示 |
| `SuccessBrush` | Green | 成功状态 |
| `ErrorBrush` | Red | 错误/危险操作 |
| `WarningBrush` | Peach | 警告 |
| `InputBackgroundBrush` | Surface0 | 输入框背景 |
| `InputBorderBrush` | Overlay0 | 输入框边框 |
| `InputFocusBorderBrush` | Mauve | 输入框聚焦边框 |
| `SeparatorBrush` | Overlay0 | 分割线 |

### Top Bar Background

顶栏当前是独立的深色区域。在 Catppuccin 分层中：
- 窗口背景 = `Base` (#1e1e2e)
- 顶栏 = `Mantle` (#181825) — 比 Base 更深，产生自然的层级区分

## Control Templates

### Button

两种变体（通过 `Style` 区分，不用 `x:Key`）：

**PrimaryButton** (显式 `Style="{StaticResource PrimaryButtonStyle}"`):
- 背景: `PrimaryAccentBrush` (Mauve)
- 文字: `TextPrimaryBrush`
- 圆角: 6px
- Hover: 背景 `SecondaryAccentBrush` (Lavender)，transition 150ms
- Pressed: 背景 `Mauve` 加深 (降低亮度)
- Disabled: 背景 `Surface0`，文字 `TextDisabledBrush`

**DefaultButton** (隐式 Style，应用于所有未指定 Style 的 Button):
- 背景: `Surface0`
- 边框: `InputBorderBrush` 1px
- 文字: `TextPrimaryBrush`
- 圆角: 6px
- Hover: 边框变 `PrimaryAccentBrush`
- Pressed: 背景变 `Surface1`
- Disabled: 同 PrimaryButton

### TextBox

- 背景: `InputBackgroundBrush`
- 边框: `InputBorderBrush` 1px
- 文字: `TextPrimaryBrush`
- Caret: `PrimaryAccentBrush`
- 圆角: 4px
- Focused: 边框变 `InputFocusBorderBrush`

### ComboBox

- 与 TextBox 一致的颜色体系
- 下拉面板背景: `ElevatedSurfaceBrush`

### ListView (单位选择器)

- 背景: `CardBackgroundBrush`
- 边框: `CardBorderBrush`
- 选中行: `Surface1` 背景 + `PrimaryAccentBrush` 左边框指示
- 交替行色: 不用（避免 TTY 感过重）

## Font Embedding

Sarasa Mono SC 嵌入方式：
1. `res/SarasaMonoSC-Regular.ttf` 复制到 `Themes/` 目录
2. `.csproj` 添加 `<Resource Include="Themes\SarasaMonoSC-Regular.ttf" />`
3. `App.xaml` 资源字典中定义 FontFamily：

```xml
<FontFamily x:Key="AppFont">pack://application:,,,/Ra3Trainer.App;component/Themes/SarasaMonoSC-Regular.ttf#Sarasa Mono SC</FontFamily>
```

4. 所有控件通过隐式 Style 引用此 FontFamily，无需逐个设置

## XAML Binding Rules (unchanged)

- 所有 Binding 路径不变
- Command 绑定不变
- DataTemplate DataType 匹配不变
- 只修改颜色属性和新增 Style 引用

## Resource Loading Strategy

**全部使用 `StaticResource`**。理由：
- 项目不做运行时主题切换
- StaticResource 在启动时一次性解析，性能优于 DynamicResource
- 若资源 Key 不存在 → 编译时/启动时即报错，而非运行时静默失败

## Compatibility & Risk

| 风险 | 缓解 |
|------|------|
| 新颜色导致对比度不足 | 使用 WCAG AA 工具验证 Text→Subtext1 在 Surface0 上 ≥ 4.5:1 |
| 字体缺失或加载失败 | WPF 自动 fallback 到系统默认字体，不会崩溃 |
| 控件模板破坏现有交互 | 仅修改视觉属性，不改 Template 结构或事件 |
| 弹窗/模态未主题化 | `ReinforcementUnitPickerWindow` 同步应用资源字典 |

## Trade-offs

1. **StaticResource vs DynamicResource**: 选 StaticResource — 放弃运行时换肤换来编译期安全检查
2. **隐式 Style vs x:Key Style**: 尽量用隐式 Style（全局统一），仅在语义差异处（如 PrimaryButton）用 Key
3. **字体内嵌 vs 系统字体**: 选内嵌 Sarasa Mono SC — 25MB 增加构建体积，换取跨设备一致的等宽终端体验
4. **BorderRadius 6px vs native corners**: 选 6px — WPF 默认控件无圆角，少量圆角显著提升现代感但不过度
