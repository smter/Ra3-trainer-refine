# 热键规范 — 全局键盘钩子

> 全局键盘钩子（`WH_KEYBOARD_LL`）的安装、事件分发、前台检测与生命周期管理。

---

## 架构概述

```
Win32 WH_KEYBOARD_LL Hook
        ↓ KeyDown/KeyUp
LowLevelKeyboardHook (App 层)
        ↓ 转发事件
HotkeyFeatureDispatcher (Core 层)
        ↓ 匹配手势 → 调用回调
FeatureItemViewModel.ExecuteFromHotkey()
        ↓ 操作
FeatureController (Core 层)
```

关键设计：只在**目标游戏窗口处于前台**时才响应热键。

---

## LowLevelKeyboardHook — Win32 钩子封装

**位置：** `src/Ra3Trainer.App/Hotkeys/LowLevelKeyboardHook.cs`

### 安装/卸载

```csharp
public sealed class LowLevelKeyboardHook : IDisposable
{
    private IntPtr _hookId = IntPtr.Zero;

    public void Install()
    {
        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule;
        _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, HookCallback,
            GetModuleHandle(module.ModuleName), 0);
    }

    public void Uninstall()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }
}
```

### 事件转发

```csharp
// HookProc（静态回调）→ KeyDown/KeyUp 事件
public event EventHandler<KeyboardHookEventArgs> KeyDown;
public event EventHandler<int> KeyUp;  // 仅 virtualKey
```

### 生命周期管理

**位置：** `src/Ra3Trainer.App/ViewModels/MainViewModel.cs:737-755`

```csharp
private void StartHotkeys()
{
    var bindings = Groups
        .SelectMany(group => group.Features)
        .Select(item => HotkeyGesture.TryParse(item.Hotkey, out var gesture)
            ? new HotkeyFeatureBinding(gesture, item.Feature, item.ExecuteFromHotkey)
            : null)
        .Where(binding => binding is not null)
        .Cast<HotkeyFeatureBinding>()
        .ToArray();

    _hotkeyDispatcher.Update(bindings, enabled: true);
    _keyboardHook.Install();
}

private void StopHotkeys()
{
    _hotkeyDispatcher.Update([], enabled: false);
    _keyboardHook.Uninstall();
}
```

**规则：**
- `StartHotkeys()` 在 patch 安装成功后调用
- `StopHotkeys()` 在 patch 恢复和 session 释放时调用
- 先更新 Dispatcher（停用所有绑定）再卸载钩子（避免卸载过程中触发回调）

---

## ForegroundWindowProcess — 前台窗口检测

**位置：** `src/Ra3Trainer.App/Hotkeys/ForegroundWindowProcess.cs`

```csharp
public sealed class ForegroundWindowProcess
{
    public int GetForegroundProcessId()
    {
        GetWindowThreadProcessId(GetForegroundWindow(), out var processId);
        return (int)processId;
    }
}
```

**使用：** `src/Ra3Trainer.App/ViewModels/MainViewModel.cs:776-779`

```csharp
private bool IsTargetGameForeground()
{
    return _targetProcessId is int targetProcessId &&
        _foregroundWindowProcess.GetForegroundProcessId() == targetProcessId;
}
```

**在钩子回调中应用：** `KeyDown` 事件处理的第一行就是前台检查——非目标窗口直接忽略。

---

## HotkeyFeatureDispatcher — 热键分发

（Core 层组件，但由 App 层驱动）

**位置：** `src/Ra3Trainer.Core/Hotkeys/HotkeyFeatureDispatcher.cs`

```csharp
// 分发逻辑
public bool TryDispatch(int virtualKey, ModifierKeys modifiers)
{
    // 1. 匹配 HotkeyGesture → HotkeyFeatureBinding
    // 2. 调用 binding.Callback (ExecuteFromHotkey)
    // 3. 返回 handled = true/false
}
```

**支持模式：**
- **持续按压**（如高速移动 `-`、缓慢移动 `=`）→ `KeyDown` 触发 + `KeyUp` 释放
- **单次触发**（如升级 `P`、摧毁 `Delete`）→ `KeyDown` 触发

---

## HotkeyGesture 解析

**位置：** `src/Ra3Trainer.Core/Hotkeys/HotkeyGesture.cs`

```csharp
// 格式：Modifiers+Key（如 "Ctrl+F1"、"Shift+A"、单键 "P"）
public static bool TryParse(string text, out HotkeyGesture gesture);
```

---

## 热键注册流程

```
1. MainViewModel 构造 → 从 TrainerFeatureCatalog 读取每个 feature 的快捷键
2. StartHotkeys() → 遍历 Groups，解析 HotkeyGesture，构建 HotkeyFeatureBinding[]
3. HotkeyFeatureDispatcher.Update(bindings, enabled: true)
4. LowLevelKeyboardHook.Install() → SetWindowsHookEx(WH_KEYBOARD_LL)
5. 运行时：KeyDown → IsTargetGameForeground()? → TryDispatch → ExecuteFromHotkey → FeatureController
6. StopHotkeys() → HotkeyFeatureDispatcher.Update([], false) → LowLevelKeyboardHook.Uninstall()
```

---

## 禁止模式

1. **禁止在外层线程调用 `Uninstall` 而不确保钩子循环退出。** 可能导致死锁或钩子残留。
2. **禁止在钩子回调中执行耗时操作。** 回调在钩子消息循环线程上运行——阻塞 = 系统全局键盘延迟。耗时分派到 `Dispatcher.Invoke`。
3. **禁止不检查前台窗口就响应热键。** 否则在浏览器中按 `Ctrl+F1` 也会触发游戏修改。
4. **禁止在程序启动时立即安装钩子。** 钩子在 **patch 安装后**才激活——没附加上游戏 = 热键无意义。
5. **禁止热键冲突。** 两个功能绑定同一快捷键视为 bug。`HotkeyFeatureDispatcher` 应去重或报错。
