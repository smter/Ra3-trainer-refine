# 质量规范（Core）

> Ra3Trainer.Core 测试标准、接口设计原则与代码风格。

---

## 测试规范

### 测试框架与结构

- **框架：** xUnit（`Ra3Trainer.Tests.csproj`）
- **命名：** `MethodName_ExpectedBehavior` — 不强制 `_WhenCondition` 后缀，简洁优先
- **结构：** AAA（Arrange → Act → Assert）

### Fake 优于 Mock

**核心原则：** 使用手写 fake 实现接口，不使用 Mock 框架（Moq/NSubstitute）。

**示例：** `tests/Ra3Trainer.Tests/FakeProcessMemory.cs`

```csharp
// 手写 fake 实现 IProcessMemory 接口
public class FakeProcessMemory : IProcessMemory
{
    private readonly Dictionary<nint, byte[]> _memory = new();

    public byte[] ReadBytes(nint address, int count)
    {
        if (_memory.TryGetValue(address, out var bytes))
            return bytes.Take(count).ToArray();
        return new byte[count];
    }

    public void WriteBytes(nint address, ReadOnlySpan<byte> bytes)
    {
        _memory[address] = bytes.ToArray();
    }
}
```

**为何不用 Mock 框架：**
- IProcessMemory 的语义（地址→字节映射）太复杂，setup/verify 代码比手写 fake 更长
- Fake 可复用，所有测试共享同一个实现
- 不引入额外 NuGet 依赖

### 参数化测试

```csharp
// 从 FeatureControllerTests.cs 的模式
[Theory]
[InlineData("Moeny", true)]           // 有 EnableFlags → 是 Toggle
[InlineData("We Need Back", false)]   // 有 ValueHint → 不是 Toggle
public void IsToggleFeature_ReturnsExpected(string rawName, bool expected)
{
    var feature = features.First(f => f.RawName == rawName);
    Assert.Equal(expected, FeatureController.IsToggleFeature(feature));
}
```

### 需要测试的层次

| 层 | 测试类型 | 示例 |
|----|---------|------|
| Codegen | 单元测试 | `AaInstructionEmitterTests` — 每条指令逐一验证编码输出 |
| Patching | 单元测试 | `PatchEngineTests` — 用 FakeProcessMemory 验证安装/恢复 |
| Features | 单元测试 | `FeatureControllerTests` — 验证 Toggle/Action 分类逻辑 |
| Runtime | 单元测试 | `TrainerSessionTests` — 验证 Attach 校验和状态转换 |
| Manifest | 单元测试 | `ManifestRepositoryTests` — 验证 JSON 反序列化 |

---

## 接口设计原则

### 接口抽象用于可测试性

所有外部依赖（进程内存、Win32 API、文件系统）都通过接口抽象：

| 接口 | 生产实现 | 测试 Fake |
|------|---------|----------|
| `IProcessMemory` | `Win32ProcessMemory` | `FakeProcessMemory` |
| `IWin32MemoryApi` | `Kernel32MemoryApi` | 不直接测试（internal） |
| `IRemoteAllocator` | `SequentialRemoteAllocator` | 直接传入基址 |
| `IProcessSuspender` | `Win32ProcessSuspender` | null（跳过挂起） |
| `ITargetProcessState` | `TargetProcessState` | 可控 fake |

**模式：** 构造函数接受接口，提供默认实现：

```csharp
// TrainerSession.cs:20-38
public TrainerSession(
    TrainerManifest manifest,
    IProcessMemory memory,
    TrainerTarget target,
    IRemoteAllocator? allocator = null,
    BootstrapCodeBuilder? codeBuilder = null,
    // ...
)
{
    _allocator = allocator ?? new SequentialRemoteAllocator(0);
    _codeBuilder = codeBuilder ?? new BootstrapCodeBuilder();
    // ...
}
```

**规则：** 可选依赖用 `?? new DefaultImpl()` 模式，生产代码无需 DI 容器，测试代码可注入 fake。

---

## Record 类型使用

### 何时用 Record

```csharp
// ✅ 数据载体 → record
public sealed record TrainerTarget(
    string ProcessName, nint ModuleBase, bool Is32Bit,
    bool VersionSupported, int? ProcessId = null);

public sealed record AttachResult(bool Success, string Message);

public sealed record BootstrapCode(
    byte[] MustCode, byte[] MustCode2,
    IReadOnlyDictionary<string, byte[]> Initializers);

// ✅ 配置/清单 → record
public sealed record TrainerManifest(
    string TargetProcess, IReadOnlyList<TrainerFeature> Features,
    PatchManifest PatchManifest, IReadOnlyList<ActionDispatchEntry> ActionDispatch);
```

### 何时用 Class

```csharp
// ✅ 有状态、有生命周期 → class
public sealed class TrainerSession : IDisposable { ... }
public sealed class PatchEngine { ... }
public sealed class FeatureController { ... }
public sealed class Win32ProcessMemory : IProcessMemory, IRemoteAllocator, IDisposable { ... }
```

**规则：** record = 不可变数据。class = 有状态行为。

---

## 代码风格

| 规则 | 说明 |
|------|------|
| `<Nullable>enable</Nullable>` | 全局开启。极少使用 `!`（null-forgiving），优先用结构设计避免 null |
| `<LangVersion>latest</LangVersion>` | 使用最新 C# 特性 |
| `IReadOnlyList<T>` 优于 `List<T>` | 公开返回类型用只读接口，防止外部意外修改 |
| 集合表达式（C# 12） | `Array.Empty<string>()` 可简化为 `[]` |
| `sealed` 默认 | 除非明确设计为继承，class 默认 sealed |
| `using` 而非手动 `Dispose` | Win32 资源通过 `SafeHandle` 管理，业务类实现 `IDisposable` |

---

## 禁止模式

1. **禁止跳过接口直接依赖实现。** 不在测试中 `new Win32ProcessMemory(pid)`——用 `FakeProcessMemory`。
2. **禁止用 `dynamic`。** 失去编译时检查，对桌面应用无任何收益。
3. **禁止在 record 中定义方法（除 `ToString` 等重写）。** Record 是数据载体，行为放在独立的 Controller/Engine 类中。
4. **禁止在测试中硬编码内存地址。** 所有地址通过 `FakeProcessMemory` 预设。
5. **禁止提交跳过测试的代码。** `dotnet test` 必须全部通过。
6. **禁止在 Core 层引入第三方库（Iced 除外）。** Core 是纯逻辑库，保持零额外依赖。
