using Ra3Trainer.Core.Features;

namespace Ra3Trainer.App.ViewModels;

public sealed class ReinforcementQueueItemViewModel : ViewModelBase
{
    private string _status = "等待";
    private string _message = string.Empty;

    public ReinforcementQueueItemViewModel(
        string name,
        string unitIdText,
        string countText,
        string rankText,
        Action<ReinforcementQueueItemViewModel> remove,
        Func<bool> canRemove)
    {
        Name = string.IsNullOrWhiteSpace(name) ? unitIdText : name.Trim();
        UnitIdText = unitIdText;
        CountText = countText;
        RankText = rankText;
        RemoveCommand = new RelayCommand(() => remove(this), canRemove);
    }

    public string Name { get; }

    public string UnitIdText { get; }

    public string CountText { get; }

    public string RankText { get; }

    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
        }
    }

    public string Message
    {
        get => _message;
        set
        {
            _message = value;
            OnPropertyChanged();
        }
    }

    public RelayCommand RemoveCommand { get; }

    public ReinforcementQueueEntry ToEntry()
    {
        return new ReinforcementQueueEntry(Name, UnitIdText, CountText, RankText);
    }

    public void ApplyResult(ReinforcementQueueResult result)
    {
        Status = result.Status switch
        {
            ReinforcementQueueItemStatus.Executed => "已执行",
            ReinforcementQueueItemStatus.Skipped => "已跳过",
            ReinforcementQueueItemStatus.TimedOut => "超时",
            ReinforcementQueueItemStatus.Failed => "失败",
            ReinforcementQueueItemStatus.Executing => "执行中",
            _ => "等待"
        };
        Message = result.Message;
    }

    public void RaiseCommandState()
    {
        RemoveCommand.RaiseCanExecuteChanged();
    }
}
