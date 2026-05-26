# 现代化 UI 设计升级

## Goal

将 RA3 Trainer 的 WPF 界面从当前硬编码浅色风格升级为基于 Catppuccin Mocha 暗色主题的现代化设计系统，提升视觉品质和用户体验。

## Confirmed Facts (from codebase inspection)

- **项目**: .NET 8 WPF (`net8.0-windows`, `UseWPF=true`, `RuntimeIdentifier=win-x86`)
- **UI 库**: 零第三方 UI 依赖（仅 `Iced` 1.21.0 用于 x86 汇编）
- **XAML 文件**: 3 个 — `App.xaml`, `MainWindow.xaml`, `Views/ReinforcementUnitPickerWindow.xaml`
- **当前状态**: `App.xaml` 的 `Application.Resources` 为空；所有颜色硬编码在 XAML 中（`#F4F5F7`, `#1F2937`, `#D7DAE0`, `#111827` 等）
- **现有规范**: `.trellis/spec/app/component-guidelines.md` 声明"项目固定浅色主题，不需要运行时换肤"（需更新）
- **MVVM**: ViewModel 无 View 引用，code-behind 仅 `InitializeComponent()` + `DataContext` 赋值
- **配色方案**: Catppuccin Mocha 25 色已在 `.trellis/spec/app/catppuccin.md` 定义
- **设计工具**: ui-ux-pro-max 完整数据在 `.claude/skills/ui-ux-pro-max/`

## Design Direction (from ui-ux-pro-max research)

- **风格**: Dark Mode (OLED) + Vibrant & Block-based 混合 — 暗色底、高对比、活力强调色
- **配色基础**: Catppuccin Mocha 25 色分层体系 (Base → Surface → Overlay → Text)
- **强调色方向**: Catppuccin 原生 — Mauve (`#cba6f7`) 主强调 / Lavender (`#b4befe`) 辅助 / Blue (`#89b4fa`) 信息
- **字体**: Inter（WPF 中可用系统字体 fallback: Segoe UI）
- **效果**: 微妙间距分层、hover 状态变换 (150-300ms)、大区块间距 (48px+)

## Requirements

### R1: 暗色主题 ResourceDictionary
- 创建 `Themes/CatppuccinMocha.xaml` 作为全局颜色资源字典
- 定义 25 色 Catppuccin Mocha 的 `Color` / `SolidColorBrush` 资源
- 定义语义化 token: `PrimaryBrush`, `SecondaryBrush`, `AccentBrush`, `SurfaceBrush`, `TextPrimaryBrush` 等
- `App.xaml` 合并该资源字典

### R2: 全局控件样式
- 为 `Button`, `TextBox`, `ComboBox`, `ListView`, `Border` 创建暗色主题隐式 Style
- 统一交互状态: hover / pressed / disabled 视觉反馈
- 统一间距体系: 基于 4/8dp 节奏

### R3: 窗口与页面迁移
- `MainWindow.xaml` — 移除所有硬编码颜色，改用 `{StaticResource}` 引用
- `ReinforcementUnitPickerWindow.xaml` — 同上
- `App.xaml` — 移除 `StartupUri` 的 `Background` 硬编码

### R4: 规范更新
- 更新 `.trellis/spec/app/component-guidelines.md` 中的主题相关声明
- 将 Catppuccin 颜色使用模式写入规范

## Acceptance Criteria

- [ ] 应用启动后全局显示 Catppuccin Mocha 暗色主题
- [ ] 所有 3 个 XAML 文件不再包含硬编码颜色值
- [ ] 按钮/输入框/下拉框/列表有统一的暗色样式
- [ ] 文字对比度 ≥ 4.5:1（WCAG AA）
- [ ] 现有功能不受影响：进程检测、Patch 安装/恢复、增援队列、单位选择器
- [ ] 窗口最小尺寸下布局不错乱

## Decisions Made

1. **强调色**: Catppuccin 原生 — Mauve `#cba6f7` 主强调 / Lavender `#b4befe` 辅助 / Blue `#89b4fa` 信息
2. **控件重塑深度**: B 级 — 颜色迁移 + 圆角 6px + Flat 按钮 + 暗色输入框 + hover/pressed 状态
3. **卡片风格**: 微边框 — Surface0 底 + Overlay0 1px 边框，保持当前结构
4. **标题栏**: 保持标准 Windows 标题栏，不做 WindowChrome 一体化
5. **字体**: 内嵌 Sarasa Mono SC (`res/SarasaMonoSC-Regular.ttf`) — 等宽字体，技术/终端感

## Out of Scope

- 运行时主题切换（亮/暗切换）—— 仅暗色主题
- 新增第三方 UI 控件库（如 wpfui, ModernWPF）
- 全量 ControlTemplate 重写（ScrollBar、ComboBox 下拉等）
- 动画系统（WPF Trigger/Storyboard 保留给后续迭代）

## Notes

- 这是复杂任务，需要 `design.md` + `implement.md`
