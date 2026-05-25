using Ra3Trainer.Core.Manifest;

namespace Ra3Trainer.Core.Features;

public sealed record ReinforcementQueueEntry(
    string Name,
    string UnitIdText,
    string CountText,
    string RankText);

public enum ReinforcementQueueItemStatus
{
    Pending,
    Executing,
    Executed,
    Skipped,
    TimedOut,
    Failed
}

public sealed record ReinforcementQueueResult(
    ReinforcementQueueEntry Entry,
    ReinforcementQueueItemStatus Status,
    string Message);

public static class ReinforcementQueueRunner
{
    public static async Task<IReadOnlyList<ReinforcementQueueResult>> ExecuteAsync(
        IEnumerable<ReinforcementQueueEntry> entries,
        FeatureController controller,
        TrainerFeature reinforcementFeature,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ReinforcementQueueResult>();
        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReinforcementSettings settings;
            try
            {
                settings = ReinforcementSettings.Parse(entry.UnitIdText, entry.CountText, entry.RankText);
            }
            catch (Exception ex)
            {
                results.Add(new ReinforcementQueueResult(entry, ReinforcementQueueItemStatus.Skipped, ex.Message));
                continue;
            }

            try
            {
                var result = await controller.TriggerActionAndWaitForConsumptionAsync(
                    reinforcementFeature,
                    settings,
                    timeout,
                    pollInterval,
                    cancellationToken: cancellationToken);
                results.Add(new ReinforcementQueueResult(entry, ToStatus(result), ToMessage(result)));
            }
            catch (Exception ex)
            {
                results.Add(new ReinforcementQueueResult(entry, ReinforcementQueueItemStatus.Failed, ex.Message));
            }
        }

        return results;
    }

    private static ReinforcementQueueItemStatus ToStatus(ActionDispatchResult result)
    {
        return result switch
        {
            ActionDispatchResult.Consumed => ReinforcementQueueItemStatus.Executed,
            ActionDispatchResult.TimedOut => ReinforcementQueueItemStatus.TimedOut,
            _ => ReinforcementQueueItemStatus.Failed
        };
    }

    private static string ToMessage(ActionDispatchResult result)
    {
        return result switch
        {
            ActionDispatchResult.Consumed => "已执行",
            ActionDispatchResult.TimedOut => "动作已写入但尚未被游戏循环消费。",
            _ => "增援功能不是可分发动作。"
        };
    }
}
