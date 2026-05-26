# 类型安全（C#）

> Ra3Trainer.App 中的 C# 类型安全约定：Nullable、Record 类型、接口抽象、集合选择。

---

## Nullable 启用

项目全局启用 `<Nullable>enable</Nullable>`。

### 设计模式：非 null 默认，可 null 显式标记

```csharp
// ✅ 大部分引用类型假设非 null
public string StatusMessage { get; set; } = "未检测进程。";

// ✅ 可选值显式标记
public FeatureController? FeatureController { get; private set; }
public TrainerSession? _session;  // 在 Attach 前为 null
private Win32ProcessMemory? _memory;
```

### 状态守卫模式

不依赖 `!`（null-forgiving operator）压制警告，而是用状态守卫确保非 null：

```csharp
// ✅ 状态检查后再访问
public void InstallPatches()
{
    if (_session is null) { StatusMessage = "请先检测进程。"; return; }
    _session.InstallPatches();  // 编译器知道这里非 null
}
```

### 可选参数默认值

```csharp
// ✅ 参数默认值用 null + ?? 回退模式
public TrainerSession(
    IRemoteAllocator? allocator = null,
    BootstrapCodeBuilder? codeBuilder = null)
{
    _allocator = allocator ?? new SequentialRemoteAllocator(0);
    _codeBuilder = codeBuilder ?? new BootstrapCodeBuilder();
}
```

---

## Record 类型用于数据

### Core 层 record（跨边界数据载体）

```csharp
// 不可变数据 → record
public sealed record TrainerTarget(
    string ProcessName, nint ModuleBase, bool Is32Bit,
    bool VersionSupported, int? ProcessId = null);

public sealed record AttachResult(bool Success, string Message);

public sealed record ReinforcementPreset(string Name, uint UnitId, int Count, int Rank);
```

### App 层数据（设置/配置）

```csharp
public record TrainerAppSettings(
    string LauncherPath, string LauncherArguments, int AttachTimeoutSeconds,
    ResourceValueSettings ResourceValues, ReinforcementPreset[] ReinforcementPresets);
```

### 何时不用 Record

```csharp
// ❌ 有状态、可变 → class（sealed）
public sealed class MainViewModel : ViewModelBase, IDisposable { ... }
public sealed class FeatureController { ... }
```

---

## 集合类型选择

| 类型 | 使用场景 |
|------|---------|
| `IReadOnlyList<T>` | 公开返回类型——只读，防止外部误修改 |
| `IReadOnlyDictionary<K,V>` | 公开返回的字典 |
| `ObservableCollection<T>` | WPF 绑定列表——自动通知 UI 增删 |
| `List<T>` | 内部临时集合（不公开） |
| `Array.Empty<T>()` / `[]` | 空集合默认值 |

**示例：**

```csharp
// ✅ 对外暴露只读
public IReadOnlyDictionary<string, nint> Symbols { get; private set; }

// ✅ WPF 绑定用 ObservableCollection
public ObservableCollection<FeatureGroupViewModel> Groups { get; }

// ✅ 内部可变集合
private readonly List<(nint Address, byte[] OriginalBytes)> _installed = new();
```

---

## 接口抽象

```csharp
// 所有外部依赖都通过接口
public interface IProcessMemory
{
    byte[] ReadBytes(nint address, int count);
    void WriteBytes(nint address, ReadOnlySpan<byte> bytes);
}

internal interface IWin32MemoryApi  // internal — 仅 Memory namespace 内可见
{
    SafeProcessHandle OpenProcess(uint desiredAccess, bool inheritHandle, int processId);
    bool ReadProcessMemory(SafeProcessHandle process, nint baseAddress, byte[] buffer, int size, out int bytesRead);
    // ...
}
```

**原则：** 接口 = 边界。所有跨边界通信通过接口，确保可测试性。

---

## 禁止模式

1. **禁止使用 `dynamic`。** 失去所有编译时类型检查，对桌面应用零收益。
2. **禁止用 `!`（null-forgiving）在非明显场景。** 如果类型系统告诉你可能为 null，用状态守卫而非 `!`。
3. **禁止用 `object` 作为 API 参数/返回值。** 用具体类型或泛型。`RelayCommand` 的 `object parameter` 是唯一例外（WPF `ICommand` 接口强制）。
4. **禁止 `List<T>` 作为公开属性类型。** 用 `IReadOnlyList<T>` 或 `ObservableCollection<T>`。
5. **禁止用 `HasValue` + `.Value` 链式访问可空类型。** 用模式匹配 `is { } value` 或 null 检查后直接访问。
