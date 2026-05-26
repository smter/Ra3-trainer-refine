# 错误处理（Core）

> Ra3Trainer.Core 中的异常类型、结果模式与错误恢复机制。

---

## 错误处理模式

### 模式 1：事务式安装 + 自动回滚

**场景：** 安装多个 hook 时，任一失败则全部恢复。

**位置：** `src/Ra3Trainer.Core/Patching/PatchEngine.cs:19-71`

```csharp
public void Install(IEnumerable<PatchHookPlan> hooks)
{
    var hookList = hooks.ToArray();
    PatchInstallException? installException = null;
    try
    {
        for (var index = 0; index < hookList.Length; index++)
        {
            var hook = hookList[index];
            try
            {
                // 校验原始字节 → 计算 patch 字节 → 写入
                var currentBytes = _memory.ReadBytes(address, hook.OriginalBytes.Length);
                if (!currentBytes.SequenceEqual(hook.OriginalBytes))
                    throw new InvalidOperationException("...");
                _memory.WriteBytes(address, patchBytes);
                AddInstalled(address, hook.OriginalBytes);
            }
            catch (Exception ex)
            {
                installException = new PatchInstallException(hook, index + 1, hookList.Length, address, ex);
                throw installException;
            }
        }
    }
    catch
    {
        try { RestoreAll(); }  // 回滚所有已安装 hook
        catch (Exception restoreEx) when (installException is not null)
        {
            throw new InvalidOperationException(
                $"{installException.Message} Rollback also failed: {restoreEx.Message}", installException);
        }
        throw;
    }
}
```

**关键要素：**
- 安装前**校验原始字节**（防御性：防止 hook 地址不匹配导致崩溃）
- 失败时**捕获并包装**为 `PatchInstallException`，附加 hook 上下文
- 外层 catch 触发 `RestoreAll()` 逆序恢复
- 回滚失败时**双重包装**异常，不丢失原始错误信息

### 模式 2：AttachResult（成功/失败结果对象）

**场景：** 附加目标进程的校验失败不需要抛出异常，返回结果对象即可。

**位置：** `src/Ra3Trainer.Core/Runtime/TrainerSession.cs:54-83`

```csharp
public AttachResult Attach()
{
    if (!TrainerProcessName.Matches(_target.ProcessName, _manifest.TargetProcess))
        return Reject("目标进程不匹配。");

    if (!_target.Is32Bit)
        return Reject("目标进程不是 32 位。");

    if (!_target.VersionSupported)
        return Reject("版本不支持，仅支持 RA3 1.12.3444.25830。");

    // 分配远程内存 → 构建符号表 → 创建 PatchEngine
    CanUseFeatures = true;
    return new AttachResult(true, "已连接 RA3 1.12.3444.25830。");
}

private AttachResult Reject(string message)
{
    CanUseFeatures = false;
    return new AttachResult(false, message);
}
```

**何时用 Result 对象 vs 异常：**
- Result 对象：**预期内**的可恢复失败（进程名不匹配、版本不支持）——调用方有多种处理方式
- 异常：**预期外**的不可恢复错误（内存分配失败、读写失败）——调用方只能放弃或重试

### 模式 3：状态守卫（InvalidOperationException）

**场景：** 操作的前置条件不满足。

**位置：** `src/Ra3Trainer.Core/Runtime/TrainerSession.cs:85-89`

```csharp
public void InstallPatches(int? maxHookCount = null)
{
    if (!CanUseFeatures || Resolver is null || _patchEngine is null)
        throw new InvalidOperationException("Attach must succeed before installing patches.");
    // ...
}
```

**状态机：**
```
未附加 → Attach() → CanUseFeatures=true → InstallPatches() → ArePatchesInstalled=true
                                                                    ↓
                                              Dispose() ← RestorePatches()
```

### 模式 4：Dispose 中的防御性 try/catch

**场景：** `Dispose` 不应抛出异常。游戏进程已退出时，恢复 hook 必然失败——这是正常情况。

**位置：** `src/Ra3Trainer.Core/Runtime/TrainerSession.cs:128-137`

```csharp
public void Dispose()
{
    try
    {
        _patchEngine?.RestoreAll();
    }
    catch when (!_targetProcessState.IsRunning(_target.ProcessId))
    {
        // 游戏已退出，地址空间已消失，hook 无法恢复——这是预期行为
    }
}
```

**关键要素：**
- 使用 `catch when` 而非空 `catch`——**精确捕获**特定条件，其他异常仍会传播
- 注释解释为什么吞异常是合理的

### 模式 5：Win32 错误转换

**场景：** Win32 API 调用失败时，将原生错误码转为 .NET 异常。

**位置：** `src/Ra3Trainer.Core/Memory/Kernel32MemoryApi.cs`（模式示例）

```csharp
// P/Invoke 声明：
[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
static extern bool ReadProcessMemory(
    SafeProcessHandle hProcess, IntPtr lpBaseAddress,
    [Out] byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

// 调用处：
if (!ReadProcessMemory(handle, address, buffer, buffer.Length, out var bytesRead))
    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
```

**规则：**
- `SetLastError = true` 在 `[DllImport]` 中必须设置
- 失败时立即调用 `Marshal.GetLastWin32Error()`（延迟调用可能被覆盖）
- 抛出 `Win32Exception` 携带原生错误码（可被上层翻译为用户友好消息）

---

## 禁止模式

1. **禁止空 catch 块。** 每个 `catch` 必须有明确的处理逻辑或注释说明为什么吞异常。唯一的例外是 `catch when` 精确条件捕获（如进程已退出）。
2. **禁止在 `Dispose` 中抛出异常。** `Dispose` 应幂等、安全。如需报告错误，使用日志或状态标志，不抛出。
3. **禁止用异常做控制流。** 预期内的分支（进程名不匹配、版本校验失败）用 `AttachResult` 等结果对象，不用 `throw`。
4. **禁止吞掉回滚异常而不报告。** 回滚失败意味着目标进程可能处于不一致状态——这是严重问题，必须记录或抛出复合异常。
5. **禁止在 `catch when` 条件中执行副作用。** 条件应为纯布尔表达式。

---

## 常见错误

| 错误 | 正确做法 |
|------|---------|
| `Marshal.GetLastWin32Error()` 在 P/Invoke 调用之后隔了多行才调用 | 立即调用，中间不插入任何系统调用 |
| `catch (Exception) { /* ignore */ }` | 用 `catch when (condition)` 精确捕获，并加注释解释 |
| 回滚逻辑放在 `catch` 块内而不在外层 `finally` | 用 `try { ... } catch { RestoreAll(); throw; }` 确保即使内层 catch 处理了异常也会回滚 |
| `throw new Exception("error")` | 用具体类型：`InvalidOperationException`, `Win32Exception`, 或自定义 `PatchInstallException` |
