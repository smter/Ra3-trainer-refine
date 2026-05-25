using Ra3Trainer.Core.Codegen;
using Xunit;

namespace Ra3Trainer.Tests;

public sealed class BootstrapCodeBuilderTests
{
    [Fact]
    public void BuildReturnsEmptyCodeWhenNoAssemblerLinesAreProvided()
    {
        var builder = new BootstrapCodeBuilder();

        var code = builder.Build(Array.Empty<string>());

        Assert.Empty(code.MustCode);
        Assert.Empty(code.MustCode2);
        Assert.Empty(code.Initializers);
    }

    [Fact]
    public void BuildFailsExplicitlyWhenBootstrapScriptRequiresAssembler()
    {
        var builder = new BootstrapCodeBuilder();

        var exception = Assert.Throws<NotSupportedException>(() => builder.Build(["MustCode:", "ret"]));

        Assert.Contains("Bootstrap build context is required", exception.Message);
    }

    [Fact]
    public void BuildWithContextEncodesRemoteBlocksAndInitializers()
    {
        var builder = new BootstrapCodeBuilder();
        var context = new BootstrapBuildContext(
            expression => expression.ToLowerInvariant() switch
            {
                "id" => 0x700000,
                "mustcode" => 0x710000,
                "mustcode+10" => 0x710010,
                "mustcode+20" => 0x710020,
                "_back" => 0x401005,
                _ => throw new InvalidOperationException($"Unexpected expression '{expression}'.")
            },
            new Dictionary<string, nint>(StringComparer.OrdinalIgnoreCase)
            {
                ["_Back"] = 0x401005
            });

        var code = builder.Build([
            "[ENABLE]",
            "define(LoadId,call MustCode+20)",
            "iEnable+24:",
            "db A0 A5 86 65",
            "MustCode+10:",
            "LoadId",
            "jmp _Back",
            "MustCode+20:",
            "mov eax,[ID]",
            "ret",
            "[disable]"
        ], context);

        Assert.Equal(new byte[] { 0xA0, 0xA5, 0x86, 0x65 }, code.Initializers["iEnable+24"]);
        Assert.Equal(0x26, code.MustCode.Length);
        Assert.Equal(0xE8, code.MustCode[0x10]);
        Assert.Equal(0xE9, code.MustCode[0x15]);
        Assert.Equal(new byte[] { 0xA1, 0x00, 0x00, 0x70, 0x00, 0xC3 }, code.MustCode[0x20..0x26]);
    }

    [Fact]
    public void BuildEncodesCompleteBootstrapScript()
    {
        var script = TestAssets.ReadBootstrapLines();
        var builder = new BootstrapCodeBuilder();
        var context = FullScriptContext();

        var code = builder.Build(script, context);

        Assert.Equal(new byte[] { 0xA0, 0xA5, 0x86, 0x65 }, code.Initializers["iEnable+24"]);
        Assert.Equal(new byte[] { 0x08, 0x00, 0x00, 0x00 }, code.Initializers["iEnable+28"]);
        Assert.Equal(new byte[] { 0x03, 0x00, 0x00, 0x00 }, code.Initializers["iEnable+2C"]);
        Assert.Equal(new byte[] { 0xA0, 0x86, 0x01, 0x00 }, code.Initializers["iEnable+30"]);
        Assert.Equal(new byte[] { 0xA0, 0x86, 0x01, 0x00 }, code.Initializers["iEnable+34"]);
        Assert.Equal(new byte[] { 0x0F, 0x00, 0x00, 0x00 }, code.Initializers["iEnable+38"]);
        Assert.True(code.MustCode.Length > 0x1200);
        Assert.True(code.MustCode2.Length > 0xC00);
        Assert.Equal(0x60, code.MustCode[0]);
        Assert.Equal(0x9C, code.MustCode[0x29]);
        Assert.Equal(0x60, code.MustCode[0x600]);
        Assert.NotEqual(0x00, code.MustCode2[0]);
        Assert.Equal(0xE8, code.MustCode2[0xC00]);
    }

    [Fact]
    public void BuildEncodesReinforcementSettingsAsRuntimeReads()
    {
        var script = TestAssets.ReadBootstrapLines();
        var builder = new BootstrapCodeBuilder();
        var context = FullScriptContext();

        var code = builder.Build(script, context);

        Assert.True(ContainsBytes(code.MustCode2, [0xA1, 0x24, 0x01, 0x70, 0x00]));
        Assert.True(ContainsBytes(code.MustCode2, [0xA1, 0x28, 0x01, 0x70, 0x00]));
        Assert.True(ContainsBytes(code.MustCode, [0xFF, 0x35, 0x2C, 0x01, 0x70, 0x00]));
    }

    [Fact]
    public void BuildEncodesResourceValuesAsRuntimeReads()
    {
        var script = TestAssets.ReadBootstrapLines();
        var builder = new BootstrapCodeBuilder();
        var context = FullScriptContext();

        var code = builder.Build(script, context);

        Assert.True(ContainsBytes(code.MustCode, [0x8B, 0x0D, 0x30, 0x01, 0x70, 0x00]));
        Assert.True(ContainsBytes(code.MustCode, [0x01, 0x48, 0x04]));
        Assert.True(ContainsBytes(code.MustCode, [0x8B, 0x0D, 0x34, 0x01, 0x70, 0x00]));
        Assert.True(ContainsBytes(code.MustCode, [0x89, 0x48, 0x04]));
        Assert.True(ContainsBytes(code.MustCode, [0x8B, 0x0D, 0x38, 0x01, 0x70, 0x00]));
        Assert.True(ContainsBytes(code.MustCode, [0x89, 0x48, 0x34]));
    }

    [Fact]
    public void BuildEncodesSlowSpeedUnitDataPointerIntoEax()
    {
        var script = TestAssets.ReadBootstrapLines();
        var builder = new BootstrapCodeBuilder();
        var context = FullScriptContext();

        var code = builder.Build(script, context);

        Assert.Equal(new byte[]
        {
            0x8B, 0x5F, 0x08,
            0x8B, 0x83, 0x38, 0x01, 0x00, 0x00,
            0x85, 0xC0
        }, code.MustCode2[0x111..0x11C]);
    }

    private static bool ContainsBytes(byte[] haystack, byte[] needle)
    {
        for (var index = 0; index <= haystack.Length - needle.Length; index++)
        {
            if (haystack.AsSpan(index, needle.Length).SequenceEqual(needle))
            {
                return true;
            }
        }

        return false;
    }

    private static BootstrapBuildContext FullScriptContext()
    {
        var labels = new Dictionary<string, nint>(StringComparer.OrdinalIgnoreCase)
        {
            ["_BackPlayerID"] = 0x4FF961,
            ["_BackPlayerMoney"] = 0xACFE03,
            ["_BackPlayerPower"] = 0xACFD15,
            ["_BackPlayerSCPoint"] = 0xACFE72,
            ["_BackPlayerHaveAllSC"] = 0xACFEBA,
            ["_BackPlayerFastBuild"] = 0x70F433,
            ["_BackPlayerFastBuild2"] = 0x70F392,
            ["_BackPlayerFastBuild3"] = 0x6F6D05,
            ["_BackPlayerSuperPower"] = 0x6EBB6F,
            ["_BackPlayerSuperPower2"] = 0x83EC1E,
            ["_BackDisableAllSuperPower"] = 0x83ECAC,
            ["_BackDisableAllSuperPower2"] = 0x70A5FC,
            ["_BackPlayerZoom"] = 0x5EC7D2,
            ["_BackPlayerMap"] = 0x7C2274,
            ["_BackSelectUnitAmmo"] = 0x52873A,
            ["_BackDangerLevel"] = 0x838ED6,
            ["_BackRestoreOreMine"] = 0x6D4939,
            ["_BackEnemyCantBuild"] = 0x70F535,
            ["_BackPlayerGodMode"] = 0x52EEE4,
            ["_BackPlayerOneKillItMode"] = 0x7651B3,
            ["_BackPlayerOneKillItModeData"] = 0x7FE71A,
            ["_BackPlayerOneKillItModeData2"] = 0x6E24E9
        };

        return new BootstrapBuildContext(
            expression =>
            {
                var parts = expression.Split('+', 2, StringSplitOptions.TrimEntries);
                var baseAddress = parts[0].ToLowerInvariant() switch
                {
                    "id" => 0x700000,
                    "ienable" => 0x700100,
                    "mustcode" => 0x710000,
                    "mustcode2" => 0x720000,
                    "ra3_1.12.game" => 0x400000,
                    _ => throw new InvalidOperationException($"Unexpected expression '{expression}'.")
                };
                return parts.Length == 1 ? baseAddress : baseAddress + AaNumberParser.ParseInt32(parts[1]);
            },
            labels);
    }
}
