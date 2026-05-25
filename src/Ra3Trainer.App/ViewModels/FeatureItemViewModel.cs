using System.Windows.Input;
using Ra3Trainer.Core.Features;
using Ra3Trainer.Core.Manifest;

namespace Ra3Trainer.App.ViewModels;

public sealed class FeatureItemViewModel : ViewModelBase
{
    private readonly MainViewModel _owner;
    private bool _enabled;
    private bool _isExecuting;
    private string _status = "未启用";

    public FeatureItemViewModel(TrainerFeature feature, MainViewModel owner)
    {
        Feature = feature;
        _owner = owner;
        Command = new RelayCommand(() => _ = ExecuteAsync(), () => _owner.ArePatchesInstalled && !_isExecuting);
    }

    public TrainerFeature Feature { get; }

    public string DisplayName => Feature.DisplayName;

    public string? Hotkey => Feature.Hotkey;

    public string ActionText => IsToggle ? (_enabled ? "关闭" : "开启") : "执行";

    public bool IsToggle => FeatureController.IsToggleFeature(Feature);

    public string Status
    {
        get => _status;
        private set
        {
            _status = value;
            OnPropertyChanged();
        }
    }

    public RelayCommand Command { get; }

    public void RaiseCommandState() => Command.RaiseCanExecuteChanged();

    public void ExecuteFromHotkey()
    {
        _ = ExecuteAsync();
    }

    private async Task ExecuteAsync()
    {
        try
        {
            var controller = _owner.FeatureController;
            if (controller is null)
            {
                Status = "未连接";
                _owner.StatusMessage = "请先检测进程并安装 patch。";
                return;
            }

            if (IsToggle)
            {
                var nextEnabled = !_enabled;
                if (nextEnabled)
                {
                    _owner.WriteResourceValuesIfNeeded(Feature);
                }

                _enabled = nextEnabled;
                controller.SetToggle(Feature, _enabled);
                Status = _enabled ? "已启用" : "已关闭";
                OnPropertyChanged(nameof(ActionText));
                return;
            }

            _owner.WriteResourceValuesIfNeeded(Feature);
            _isExecuting = true;
            Command.RaiseCanExecuteChanged();
            Status = Feature.DispatchTarget is null ? "已触发" : "触发中";

            var result = IsReinforcementFeature
                ? await controller.TriggerActionAndWaitForConsumptionAsync(Feature, _owner.GetReinforcementSettings())
                : await controller.TriggerActionAndWaitForConsumptionAsync(Feature);

            if (result == ActionDispatchResult.Consumed)
            {
                Status = "已执行";
            }
            else if (result == ActionDispatchResult.TimedOut)
            {
                Status = "超时";
                _owner.StatusMessage = "动作已写入但尚未被游戏循环消费。";
            }
            else
            {
                Status = "已触发";
            }
        }
        catch (Exception ex)
        {
            Status = "失败";
            _owner.StatusMessage = ex.Message;
        }
        finally
        {
            _isExecuting = false;
            Command.RaiseCanExecuteChanged();
        }
    }

    private bool IsReinforcementFeature =>
        Feature.RawName.Equals("We Need Back", StringComparison.Ordinal) ||
        string.Equals(Feature.DispatchTarget, "MustCode2+B00", StringComparison.OrdinalIgnoreCase);
}
