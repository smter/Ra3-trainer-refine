# 填充 Trellis 项目开发规范 (spec)

## Goal

将 `.trellis/spec/` 从通用 Web 全栈模板重写为本项目（.NET 8 WPF 桌面游戏修改器）的实际技术规范，确保 `trellis-implement` 和 `trellis-check` 子代理产出与项目风格一致的代码。

## Background

项目 `trellis init` 生成 12 个占位符 spec 文件，模板预设为 React + .NET Web API + EF Core，与本项目 C# WPF MVVM + Win32 P/Invoke + x86 代码生成的栈完全不匹配。`00-bootstrap-guidelines` task 也因此无法完成。

## Requirements

### R1: 目录重命名
- `spec/backend/` → `spec/core/`（对应 `Ra3Trainer.Core`）
- `spec/frontend/` → `spec/app/`（对应 `Ra3Trainer.App`）

### R2: Core spec 重写（5 个文件）
| 文件 | 动作 | 内容要点 |
|------|------|---------|
| `core/index.md` | 重写 | Core 层概述、6 namespace 索引 |
| `core/directory-structure.md` | 重写 | Codegen / Features / Manifest / Memory / Patching / Runtime 布局与职责 |
| `core/error-handling.md` | 重写 | PatchInstallException、AttachResult、InvalidOperationException、try/finally 屏障 |
| `core/quality-guidelines.md` | 重写 | 测试结构（FakeProcessMemory、AAA）、接口抽象、record 类型、nullable |
| `core/logging-guidelines.md` | 全新编写 | 日志级别、结构化格式、敏感数据规避、ILogger 集成建议 |

### R3: 删除无关 spec
- `core/database-guidelines.md` — 项目无数据库，删除

### R4: App spec 重写（6 个文件）
| 文件 | 动作 | 内容要点 |
|------|------|---------|
| `app/index.md` | 重写 | App 层概述、目录索引 |
| `app/directory-structure.md` | 重写 | Views/ViewModels/Hotkeys 布局 |
| `app/component-guidelines.md` | 重写 | WPF Window/XAML 模式、code-behind 最小原则 |
| `app/state-management.md` | 重写 | ViewModelBase、ObservableCollection、RelayCommand、单一大 VM 协调器 |
| `app/hook-guidelines.md` | 重写 | LowLevelKeyboardHook、ForegroundWindowProcess、HotkeyFeatureDispatcher |
| `app/type-safety.md` | 重写 | Nullable enable、record 类型、IReadOnlyList、接口抽象 |
| `app/quality-guidelines.md` | 重写 | XAML 规范、禁止 VM 引用 UI 命名空间、测试命名 |

### R5: Guides 保留
`spec/guides/` 3 个文件已预填充，内容通用。仅更新 `index.md` 中交叉引用。

### R6: 内容质量要求
- 每个 spec 文件 ≥ 2 个真实代码示例（带文件路径引用）
- 每个 spec 文件 ≥ 3 条"禁止模式"或"常见错误"
- 文档语言：中文（匹配项目现有文档风格）

## Acceptance Criteria

- [ ] `spec/backend/` 目录已删除，替换为 `spec/core/`
- [ ] `spec/frontend/` 目录已删除，替换为 `spec/app/`
- [ ] `spec/core/database-guidelines.md` 已删除
- [ ] 每个 spec 文件包含 ≥ 2 个真实代码示例
- [ ] 每个 spec 文件列出 ≥ 3 条禁止模式
- [ ] `core/index.md` 和 `app/index.md` 索引表准确
- [ ] `spec/guides/index.md` 交叉引用无误

## Non-Goals

- 不修改 `.trellis/scripts/`
- 不修改 `.trellis/workflow.md`
- 不修改项目源代码
- 不修改 `00-bootstrap-guidelines` task
