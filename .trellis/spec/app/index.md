# App 开发规范（Ra3Trainer.App）

> Ra3Trainer.App 项目的编码约定与最佳实践。本层是 WPF 桌面壳，纯展示与调度，不含游戏修改逻辑。

---

## 概述

`Ra3Trainer.App` 是一个 .NET 8 WPF 项目（`<OutputType>WinExe</OutputType>`，`<UseWPF>true</UseWPF>`），以 `win-x86` RuntimeIdentifier 构建（匹配 32 位目标进程）。

职责：
- WPF 窗口与用户交互
- MVVM 数据绑定与命令调度
- 全局键盘钩子（热键支持）
- 设置持久化（JSON 文件）

依赖：`Ra3Trainer.Core`（项目引用）+ `Iced`（NuGet）。

---

## 目录索引

| 目录 | 职责 |
|------|------|
| `Views/` | XAML + code-behind 文件 |
| `ViewModels/` | MVVM ViewModel 层 |
| `Hotkeys/` | 全局键盘钩子（Win32 `WH_KEYBOARD_LL`） |

---

## 规范索引

| 规范 | 说明 |
|------|------|
| [目录结构](./directory-structure.md) | Views/ViewModels/Hotkeys 组织 |
| [组件规范](./component-guidelines.md) | WPF Window/XAML 模式 |
| [状态管理](./state-management.md) | MVVM ViewModelBase + RelayCommand + ObservableCollection |
| [热键规范](./hook-guidelines.md) | 全局键盘钩子模式 |
| [类型安全](./type-safety.md) | C# Nullable / Record / 接口抽象 |
| [质量规范](./quality-guidelines.md) | XAML 规范、禁止模式、测试标准 |
