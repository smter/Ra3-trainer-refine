# Core 开发规范（Ra3Trainer.Core）

> Ra3Trainer.Core 项目的编码约定与最佳实践。本层包含所有游戏修改逻辑，零 UI 依赖。

---

## 概述

`Ra3Trainer.Core` 是一个 .NET 8 类库项目，负责：

- 解析 Cheat Engine AutoAssembler 脚本并生成 x86 机器码
- 读写目标游戏进程内存（Win32 P/Invoke）
- 安装/管理/恢复 inline hook
- 提供功能控制接口（开关/动作/资源）
- 管理进程生命周期（启动/定位/附加/等待）

依赖：仅 `Iced`（x86 汇编器库），无其他第三方依赖。

---

## Namespace 索引

| Namespace | 职责 | 关键类型 |
|-----------|------|---------|
| `Codegen` | AA 脚本解析 → x86 机器码生成 | `BootstrapCodeBuilder`, `AaInstructionEmitter`, `AaScriptReader` |
| `Features` | 游戏功能控制接口 | `FeatureController`, `ReinforcementQueueRunner`, `TrainerFeatureCatalog` |
| `Manifest` | 数据结构定义与 JSON 反序列化 | `TrainerManifest`, `PatchManifest`, `TrainerFeature` |
| `Memory` | 进程内存读写抽象 | `IProcessMemory`, `Win32ProcessMemory`, `AddressResolver` |
| `Patching` | Hook 安装/校验/恢复引擎 | `PatchEngine`, `PatchHookPlanner`, `X86PatchEncoder` |
| `Runtime` | 进程生命周期管理 | `TrainerSession`, `TrainerProcessLocator`, `GameLauncher` |

---

## 规范索引

| 规范 | 说明 |
|------|------|
| [目录结构](./directory-structure.md) | Namespace 组织与文件布局 |
| [错误处理](./error-handling.md) | 异常类型、结果模式、try/finally 屏障 |
| [质量规范](./quality-guidelines.md) | 测试标准、接口设计、代码风格 |
| [日志规范](./logging-guidelines.md) | 日志级别、结构化格式、敏感数据规避 |
