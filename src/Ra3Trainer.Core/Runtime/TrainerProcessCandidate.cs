namespace Ra3Trainer.Core.Runtime;

public sealed record TrainerProcessCandidate(
    int ProcessId,
    string ProcessName,
    string ModuleName,
    string ModulePath,
    nint ModuleBase,
    bool Is32Bit,
    string FileVersion);
