# 日志规范（Core）

> Ra3Trainer.Core 日志策略：当前状态、建议方向与敏感数据规避。

---

## 当前状态

项目目前**没有结构化日志系统**。Core 层通过以下方式报告状态：

- `AttachResult.Message` — 附加结果字符串（传递给 App 层 `StatusMessage`）
- `TrainerSession.RemoteSymbolSummary` — 远程符号调试摘要
- 异常消息（`PatchInstallException.Message`、`InvalidOperationException.Message`）

App 层使用 `MainViewModel.StatusMessage`（单一字符串）作为用户可见状态。

## 建议方向

随着项目发展，建议引入结构化日志。推荐两种方案：

### 方案 A：轻量自建（推荐）

适用场景：日志量小（< 100 条/会话），不需要远程采集。

```csharp
// 在 Core 层定义
public interface ITrainerLogger
{
    void Info(string message, params (string Key, object Value)[] properties);
    void Warn(string message, params (string Key, object Value)[] properties);
    void Error(string message, Exception? ex = null, params (string Key, object Value)[] properties);
    void Debug(string message, params (string Key, object Value)[] properties);
}
```

调用示例：
```csharp
_logger.Info("Patches installed",
    ("hookCount", InstalledHookCount),
    ("symbols", RemoteSymbolSummary));

_logger.Warn("Restore skipped, process exited",
    ("processId", _target.ProcessId));

_logger.Error("Patch install failed", ex,
    ("hookAddress", hook.Address),
    ("hookIndex", index));
```

### 方案 B：Microsoft.Extensions.Logging

适用场景：需要与 .NET 生态工具集成（Application Insights、Serilog 等）。

```csharp
// 在 TrainerSession 中注入
private readonly ILogger<TrainerSession> _logger;

// 结构化日志
_logger.LogInformation("Patches installed. HookCount={HookCount}, Symbols={Symbols}",
    InstalledHookCount, RemoteSymbolSummary);

_logger.LogWarning("Restore skipped. Process {ProcessId} already exited", processId);

_logger.LogError(ex, "Patch install failed at {HookAddress} ({Index}/{Total})",
    hook.Address, index, total);
```

### 选择建议

- 项目 < 1000 行日志/天 → 方案 A（零依赖，简单）
- 需要远程诊断/告警/仪表盘 → 方案 B

---

## 日志级别使用指南

| 级别 | 适用场景 | 示例 |
|------|---------|------|
| **Debug** | 开发调试信息（默认不输出） | "Resolve symbol MustCode+800 → 0x02A30000" |
| **Info** | 关键生命周期事件 | "Attach succeeded", "Patches installed (N hooks)", "Session disposed" |
| **Warn** | 预期内异常但可恢复 | "Process already exited, restore skipped", "Original bytes mismatch (already patched)" |
| **Error** | 不可恢复错误 | "Patch install failed at 0x...", "VirtualAllocEx returned null" |

---

## 什么应该记录

| 事件 | 级别 | 附加属性 |
|------|------|---------|
| 进程附加成功 | Info | processName, moduleBase, is32Bit, fileVersion |
| 进程附加失败 | Warn | 失败原因（name mismatch / not 32-bit / version unsupported） |
| Patch 安装完成 | Info | hookCount, symbolSummary（仅符号名，不含地址值） |
| 单个 hook 安装失败 | Error | hook address (base+offset 格式), hook index, 原始字节, 实际字节 |
| Patch 回滚 | Info | restoredHookCount |
| Patch 回滚失败 | Error | failedHookAddress, 双重异常消息 |
| Feature toggle 变更 | Debug | featureName, enabled state |
| 资源值写入 | Debug | money/power/scPoint（仅值，不含地址） |
| Session 释放 | Info | installedHookCount（释放时仍有 hook 未恢复是异常 → Warn） |

---

## 什么**不应该**记录

| 禁止项 | 原因 |
|--------|------|
| **远程内存地址的绝对值** | 安全敏感。改用 base+offset 格式（如 `ra3_1.12.game+FF95B`） |
| **完整内存内容（byte[] dump）** | 体积大，无诊断价值（原始字节校验失败时记录前后 16 字节即可） |
| **进程完整路径** | 用户隐私。只记录文件名（`ra3_1.12.game`） |
| **用户设定的资源值（高精度）** | 无诊断价值。记录数量级即可（如 "money ~100000"） |
| **键盘输入内容** | 隐私风险。键盘钩子只处理热键组合，不记录按键序列 |
| **PII（个人身份信息）** | 本地桌面应用原则上不存在 PII，但启动器路径可能含用户名（`C:\Users\xxx\...`），不应记录 |

---

## 未来集成建议

### 在 TrainerSession 中接入日志

```csharp
public sealed class TrainerSession : IDisposable
{
    private readonly ITrainerLogger _logger;  // 或 ILogger<TrainerSession>

    public TrainerSession(
        TrainerManifest manifest,
        IProcessMemory memory,
        TrainerTarget target,
        ITrainerLogger? logger = null,  // 可选，向后兼容
        // ...
    )
    {
        _logger = logger ?? NullTrainerLogger.Instance;
    }
}
```

### App 层日志输出

App 层可将 Core 层的日志事件桥接到两个目标：
1. **文件日志** — 写入 `%LocalAppData%\Ra3Trainer\logs\`，用于崩溃后诊断
2. **UI 状态栏** — Info/Warn 级别的事件同步到 `StatusMessage`，Error 级别弹窗提示

### 滚动策略

- 单文件 `trainer-{date}.log`，每日滚动
- 保留最近 7 天
- 不压缩（文件小）

---

## 禁止模式

1. **禁止 `Console.WriteLine` 做日志。** 桌面应用无控制台，输出丢失。
2. **禁止用 `MessageBox.Show` 报告 Core 层错误。** Core 层不应引用任何 UI 类型。错误通过异常或日志接口传递。
3. **禁止记录远程进程的绝对内存地址。** 用 base+offset 符号格式。
4. **禁止记录用户文件系统路径。** 启动器路径等含个人信息，仅记录文件名。
