namespace Ra3Trainer.Core.Runtime;

public interface IProcessSuspender
{
    IDisposable Suspend(int processId);
}
