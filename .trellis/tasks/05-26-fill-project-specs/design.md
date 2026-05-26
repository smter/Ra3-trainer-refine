# Design: Spec 文件结构与内容映射

## 决策

### D1: 目录命名
- `backend/` → `core/`：对应 `Ra3Trainer.Core` 项目，包含所有游戏修改逻辑
- `frontend/` → `app/`：对应 `Ra3Trainer.App` 项目，WPF 桌面壳

### D2: 不匹配模板的处理
- `database-guidelines.md`：删除。项目零数据库，保留会造成误导
- `hook-guidelines.md`：重写为 Hotkey 指南（全局键盘钩子），而非 React hooks
- `component-guidelines.md`：重写为 WPF Window/XAML 模式，而非 React 组件
- `state-management.md`：重写为 MVVM 模式，而非 Redux/Zustand/React Query
- `type-safety.md`：重写为 C# nullable + record 类型，而非 TypeScript/Zod

### D3: 文件结构约定
每个 spec 文件统一格式：
```markdown
# Title
> 一句话描述
---
## Overview
<!-- 本项目实际做法 -->
## [Pattern Section]
<!-- 真实代码示例 + 文件路径引用 -->
## Forbidden Patterns
<!-- ≥ 3 条 -->
## Common Mistakes
<!-- 从代码 defensive code 反推 -->
```

### D4: 代码示例提取策略
从以下文件提取真实示例：
- `TrainerSession.cs` — Attach/InstallPatches 编排、符号分配、事务式 rollback
- `FeatureController.cs` — Toggle/Action 模式、dispatch 轮询
- `PatchEngine.cs` — 事务式 hook 安装/恢复、原始字节校验
- `BootstrapCodeBuilder.cs` — AA 脚本 → 机器码流程
- `AaInstructionEmitter.cs` — Iced Assembler 用法
- `MainViewModel.cs` — MVVM 单一大 VM、RelayCommand、ObservableCollection
- `MainWindow.xaml` — XAML 绑定模式
- `Kernel32MemoryApi.cs` — P/Invoke 声明
- `Win32ProcessMemory.cs` — SafeHandle + Read/WriteProcessMemory
- `FakeProcessMemory.cs` — 测试 fake 模式

### D5: 审查错误规避
从 `guides/index.md` 的 AI Code Reviewer False-Positive Patterns 中记取：
- **Trust boundary confusion**：`trainer_report.json` 和 `00_bootstrap.aa` 是嵌入的资源，编译时打包，不是外部不可信输入——不在 spec 中将其标注为"需要校验的外部输入"
- **Ignoring design comments**：代码中故意的设计选择不作为 bug 标记
- **Variable misreading**：地址符号（如 `MustCode+800`）的解析逻辑是经过验证的，不标注为"魔术数字"

## 内容大纲

### core/index.md
```markdown
# Core Development Guidelines (Ra3Trainer.Core)
> Ra3Trainer.Core 项目编码规范
---
## Overview
## Namespace Index (6 个 namespace 的概述表)
## Guidelines Index (子文件链接表)
```

### core/directory-structure.md
```markdown
# Directory Structure (Core)
## Overview — Ra3Trainer.Core 目录树
## Namespace Responsibilities
  - Codegen/ — AA 脚本 → x86 机器码
  - Features/ — 游戏功能控制接口
  - Manifest/ — 数据结构与反序列化
  - Memory/ — 进程内存读写抽象
  - Patching/ — Hook 引擎
  - Runtime/ — 进程生命周期管理
## Naming Conventions
## 代码示例
```

### core/error-handling.md
```markdown
# Error Handling (Core)
## Error Types
  - PatchInstallException (含 hook 上下文和 rollback)
  - AttachResult (bool + string, 成功/失败模式)
  - InvalidOperationException (状态守卫)
## Try/Catch/Finally 屏障模式
## 禁止模式 (如: 吞异常、空 catch)
## 代码示例 (PatchEngine.Install 的事务式 try/catch)
```

### core/quality-guidelines.md
```markdown
# Quality Guidelines (Core)
## Test Patterns — FakeProcessMemory、AAA、xUnit
## Interface Design — IProcessMemory 等抽象
## Record Types — 不可变数据
## Nullable — enable + 合理使用
## 禁止模式 — 硬编码地址、跳过版本校验
## 代码示例
```

### core/logging-guidelines.md
```markdown
# Logging Guidelines (Core)
## Overview — 当前状态 + 建议方向
## Log Levels — Debug/Info/Warning/Error 使用场景
## Structured Logging — 推荐格式与字段
## What to Log — 生命周期事件、patch 状态变更
## What NOT to Log — 远程地址具体值、敏感路径
## 集成建议 — ILogger 或自定义轻量方案
```

### app/index.md
```markdown
# App Development Guidelines (Ra3Trainer.App)
## Overview
## Directory Index
## Guidelines Index
```

### app/directory-structure.md
```markdown
# Directory Structure (App)
## Views/ — XAML + code-behind
## ViewModels/ — MVVM 层
## Hotkeys/ — 键盘钩子
```

### app/component-guidelines.md
```markdown
# Component Guidelines (WPF)
## Window/XAML Pattern
## DataContext 赋值
## code-behind 最小原则
## XAML 绑定模式
## 禁止模式
```

### app/state-management.md
```markdown
# MVVM State Management
## ViewModelBase (INotifyPropertyChanged)
## ObservableCollection
## RelayCommand (ICommand)
## 单一大 VM 协调器
## 禁止模式
```

### app/hook-guidelines.md
```markdown
# Hotkey Guidelines
## LowLevelKeyboardHook (WH_KEYBOARD_LL)
## ForegroundWindowProcess 前台检测
## HotkeyFeatureDispatcher 分发
## HotkeyGesture 解析
## 生命周期 (Install/Uninstall)
```

### app/type-safety.md
```markdown
# Type Safety (C#)
## Nullable enable
## Record types
## IReadOnlyList vs List
## Interface abstractions
## Forbidden Patterns (any/dynamic)
```

### app/quality-guidelines.md
```markdown
# Quality Guidelines (App)
## XAML — 不引用 System.Windows 在 VM 中
## Code-behind — 零业务逻辑
## Test naming
## 禁止模式
```
