# 目录结构（Core）

> Ra3Trainer.Core 项目的 namespace 组织与文件布局。

---

## 目录布局

```
src/Ra3Trainer.Core/
├── Assets/                     # 嵌入资源（AA 脚本、单位代码、manifest JSON）
│   ├── 00_bootstrap.aa         # Cheat Engine AutoAssembler 原始脚本
│   ├── code.txt                # 单位名称与代码映射表
│   └── trainer_report.json     # 从原 Trainer.exe 提取的结构化数据
├── Codegen/                    # AA 脚本 → x86 机器码
│   ├── AaBlockLayout.cs        # 将编码块布局到 MustCode/MustCode2 内存区
│   ├── AaInstructionEmitter.cs # 基于 Iced 的 x86 指令编码器
│   ├── AaNumberParser.cs       # 数字字面量解析（hex/dec 混合格式）
│   ├── AaScriptReader.cs       # AA 脚本结构解析（块、符号、初始化器）
│   └── BootstrapCodeBuilder.cs # 编译入口：AA 脚本 → BootstrapCode
├── Features/                   # 游戏功能控制接口
│   ├── ActionDispatchResult.cs # 动作派发结果枚举
│   ├── FeatureController.cs    # 核心功能控制器（Toggle/Action/Resource）
│   ├── FeatureScanPlanner.cs   # 功能扫描规划
│   ├── ReinforcementPreset.cs  # 增援预设数据结构
│   ├── ReinforcementQueueRunner.cs # 增援队列执行
│   ├── ReinforcementSettings.cs    # 增援参数解析
│   ├── ReinforcementUnitCatalog.cs # 单位目录（从 code.txt 加载）
│   ├── ResourceValueSettings.cs    # 资源值参数解析
│   ├── TrainerFeatureCatalog.cs    # 功能 UI 映射（原始名→中文名+快捷键）
│   └── UnitCodeParser.cs           # 单位代码解析
├── Hotkeys/                    # 热键抽象（Core 层：无 Win32 依赖）
│   ├── HotkeyFeatureDispatcher.cs  # 热键→功能分发
│   └── HotkeyGesture.cs            # 热键手势解析
├── Manifest/                   # 数据结构定义与 JSON 反序列化
│   ├── TrainerManifest.cs      # 完整 manifest record 类型（含 DTO）
│   └── TrainerManifestRepository.cs # 从嵌入资源加载 manifest
├── Memory/                     # 进程内存读写抽象
│   ├── IProcessMemory.cs       # 核心接口：ReadBytes / WriteBytes
│   ├── IRemoteAllocator.cs     # 远程内存分配接口
│   ├── IRemoteMemoryProtector.cs   # 远程内存保护接口
│   ├── IWin32MemoryApi.cs      # Win32 API 抽象接口（可测试性）
│   ├── Kernel32MemoryApi.cs    # Kernel32 P/Invoke 实现
│   ├── SequentialRemoteAllocator.cs # 顺序远程分配器
│   ├── AddressResolver.cs      # 符号表达式 → 绝对地址
│   ├── Win32MemoryRegion.cs    # VirtualQueryEx 结果封装
│   └── Win32ProcessMemory.cs   # 完整进程内存实现（SafeHandle + Read/Write/Alloc）
├── Patching/                   # Hook 引擎
│   ├── PatchEngine.cs          # Hook 安装/恢复（事务式）
│   ├── PatchHookImageVerifier.cs   # PE 映像校验
│   ├── PatchHookInspector.cs       # Hook 状态检查
│   ├── PatchHookPlan.cs            # Hook 计划数据结构
│   ├── PatchHookPlanner.cs         # Manifest → Hook 计划
│   ├── PatchInstallException.cs    # Hook 安装异常（含上下文）
│   ├── PeImage.cs                  # PE 文件解析
│   ├── OriginalByteParser.cs       # 原始汇编字节解析
│   ├── RestoreAssemblyEncoder.cs   # 恢复代码生成
│   ├── X86AssemblyLength.cs        # x86 指令长度计算
│   └── X86PatchEncoder.cs          # JMP near 跳转编码（E9 指令）
├── Runtime/                    # 进程生命周期管理
│   ├── AttachResult.cs         # 附加结果（bool + 消息）
│   ├── DiagnosticTimeoutBudget.cs  # 诊断超时预算
│   ├── GameLauncher.cs          # RA3.exe 启动器
│   ├── GameProcessWaiter.cs     # 异步等待目标进程出现
│   ├── IProcessSuspender.cs     # 进程挂起接口
│   ├── ITargetProcessState.cs   # 目标进程状态接口
│   ├── ProcessStabilityMonitor.cs   # 进程稳定性监控
│   ├── RemoteStateLayout.cs     # 远程状态布局常量（符号地址定义）
│   ├── TrainerAppSettings.cs    # 应用设置数据结构
│   ├── TrainerProcessCandidate.cs  # 进程候选
│   ├── TrainerProcessLocator.cs # 进程查找与校验（名称+32位+版本）
│   ├── TrainerProcessName.cs    # 进程名匹配工具
│   ├── TrainerRuntimeAssets.cs  # 嵌入资源加载
│   ├── TrainerSession.cs        # 核心编排器（Attach→InstallPatches→Dispose）
│   ├── TrainerTarget.cs         # 目标进程 record（名称/基址/32位/版本）
│   └── Win32ProcessSuspender.cs # Win32 进程挂起实现
├── Ra3Trainer.Core.csproj      # 项目文件（net8.0-windows, Iced 依赖）
└── Properties/
    └── AssemblyInfo.cs
```

## Namespace 职责与边界

### Codegen — AA 脚本 → x86 机器码

**职责：** 解析 `00_bootstrap.aa` Cheat Engine AutoAssembler 脚本，使用 Iced 库将自定义 x86 汇编指令编码为机器码。

**依赖：** `Iced`（NuGet），无项目内依赖。

**边界：** 不访问目标进程内存。产物是 `byte[]`，由调用方写入远程进程。

**关键流程：**
```
AaScriptReader.Read(lines) → AaDocument (块/符号/初始化器)
    → AaInstructionEmitter.Encode(lines, origin, context) → byte[]
    → AaBlockLayout.Build(region, capacity, blocks) → byte[]
→ BootstrapCode (MustCode, MustCode2, Initializers)
```

### Features — 游戏功能控制接口

**职责：** 提供修改器功能的运行时接口（Toggle 开关 / Action 动作 / Resource 资源）。

**依赖：** `Memory`（IProcessMemory + AddressResolver）、`Runtime`（RemoteStateLayout）。

**边界：** 不存在游戏逻辑——仅写内存标志位触发远程代码。不含有 UI 代码。

**特征分类：**
- **ToggleFeature：** 有 EnableFlags 或 ToggleBytePatches，无 ValueHint（如"无限电力"）
- **ActionFeature：** 有 ValueHint（如"呼叫战场增援"），可选 DispatchTarget

### Manifest — 数据结构定义

**职责：** 定义不可变数据 record 类型，从嵌入的 `trainer_report.json` 反序列化。

**依赖：** `System.Text.Json`。

**边界：** 纯数据层，零行为逻辑。DTO record 标记 `internal`。

### Memory — 进程内存读写抽象

**职责：** 通过 Win32 P/Invoke（Kernel32）读写目标进程内存，提供可测试的接口抽象。

**依赖：** `Microsoft.Win32.SafeHandles`。

**边界：** `IProcessMemory` 是唯一对外暴露的接口。所有 Win32 细节封装在 `Kernel32MemoryApi`（`internal interface IWin32MemoryApi`）。

**设计原则：** 接口在前，实现在后。`FakeProcessMemory`（测试）和 `Win32ProcessMemory`（生产）共享同一接口。

**代码示例：** `src/Ra3Trainer.Core/Memory/IProcessMemory.cs`
```csharp
public interface IProcessMemory
{
    byte[] ReadBytes(nint address, int count);
    void WriteBytes(nint address, ReadOnlySpan<byte> bytes);
}
```

### Patching — Hook 引擎

**职责：** 将 Cheat Engine 风格的内联 hook 安装到目标进程，支持事务式安装/回滚。

**依赖：** `Memory`（IProcessMemory + AddressResolver）、`Manifest`（PatchManifest）。

**边界：** 不决定 hook 内容——hook 内容由 Manifest 和 Codegen 提供。只负责安装/校验/恢复。

**核心保证：** 任一 hook 安装失败 → 全部已安装 hook 回滚（`RestoreAll` 逆序恢复）。

**代码示例：** `src/Ra3Trainer.Core/Patching/PatchEngine.cs`
```csharp
// 安装前校验 PE 映像完整性
var verifier = new PatchHookImageVerifier(_memory);
foreach (var plan in plans) verifier.Verify(plan);

// 事务式安装：任一失败 → RestoreAll() 逆序回滚
engine.Install(plans);
```

### Runtime — 进程生命周期管理

**职责：** 管理目标游戏进程的完整生命周期：启动、查找、附加、patch、断开。

**依赖：** 几乎所有其他 namespace（编排层）。

**边界：** `TrainerSession` 是 Core 层对外的唯一入口。`Record` 类型（`TrainerTarget`、`AttachResult`）用于跨边界通信。

## 命名约定

| 规则 | 示例 |
|------|------|
| 接口以 `I` 前缀 | `IProcessMemory`, `IRemoteAllocator`, `IWin32MemoryApi` |
| `internal` 接口不对外暴露 | `IWin32MemoryApi`（仅 Memory namespace 内使用） |
| Record 类型用于数据 | `TrainerManifest`, `TrainerTarget`, `AttachResult`, `BootstrapCode` |
| Controller/Engine/Builder 后缀 | `FeatureController`（控制）、`PatchEngine`（引擎）、`BootstrapCodeBuilder`（构建器） |
| 文件与类一一对应 | `PatchEngine.cs` 只含 `PatchEngine` 类 |

## 禁止模式

1. **禁止跨 namespace 直接访问 internal 类型。** `IWin32MemoryApi` 是 internal，只在 Memory namespace 内可用。App 层不应知道 Kernel32 的存在。
2. **禁止在 Core 层引用 `System.Windows` 或任何 WPF 类型。** Core 是纯逻辑库，零 UI 依赖。
3. **禁止硬编码内存地址。** 所有地址通过 `AddressResolver` 和 `RemoteStateLayout` 符号引用解析。
4. **禁止跳过 `IProcessMemory` 接口直接调用 Kernel32。** 所有内存操作通过接口，确保可测试性。
