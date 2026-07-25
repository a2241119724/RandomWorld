namespace LAB2D.Gameplay
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Item;
    using System;
    using LAB2D.Domain.Common;
    using System.Collections.Generic;

    /// <summary>
    /// 玩家物品收集统计与里程碑提示管理器。
    /// 在玩家拾取物品时记录收集数据，并在达到预设阈值时通过 Tip 系统给予即时正向反馈。
    /// 不修改存档、Scene、Prefab 或 ScriptableObject，仅通过全局单例暴露数据和事件。
    ///
    /// 接入方式：在 ItemMap.OnTriggerEnter2D 中每次 AddItem 后调用 RecordItemCollected。
    /// 后续 UI 可订阅 MilestoneReached 事件展示里程碑动画。
    /// </summary>
    public class ItemCollectionTracker : Singleton<ItemCollectionTracker>
    {
        /// <summary>收集数量里程碑阈值（不可变）</summary>
        private static readonly int[] MilestoneThresholds =
        {
            1, 5, 10, 25, 50, 100, 200, 500, 1000, 2000, 5000, 10000,
        };

        /// <summary>已触达的里程碑阈值集合（用于去重）</summary>
        private readonly HashSet<int> reachedMilestones = new HashSet<int>();

        /// <summary>累计收集物品总数</summary>
        private int totalCollected;
        private IGameLogger gameLogger;

        private IGameLogger GameLogger => this.gameLogger ?? (this.gameLogger = Core.ServiceLocator.Get<IGameLogger>());

        /// <summary>里程碑触达事件（参数：里程碑值, 当前累计总数）</summary>
        public event Action<int, int> MilestoneReached;

        /// <summary>当前累计收集物品总数</summary>
        public int TotalCollected
        {
            get { return this.totalCollected; }
        }

        /// <summary>
        /// 记录一次物品收集。
        /// 由 ItemMap.OnTriggerEnter2D 或其他物品拾取入口调用。
        /// </summary>
        /// <param name="resourceInfo">被收集物品的资源信息（Id + Count）</param>
        /// <param name="source">收集来源标识，默认 "MapPickup"</param>
        public void RecordItemCollected(ResourceInfo resourceInfo, string source = "MapPickup")
        {
            if (resourceInfo == null || resourceInfo.Count <= 0)
            {
                return;
            }

            // 同步记录到全局会话统计（补齐 RecordItemCollected 死代码调用）
            Core.ServiceLocator.Get<GameplaySessionStats>().RecordItemCollected(resourceInfo, source);

            this.totalCollected += resourceInfo.Count;
            this.CheckMilestones();
        }

        /// <summary>检查是否已到达指定里程碑阈值</summary>
        public bool IsMilestoneReached(int threshold)
        {
            return this.reachedMilestones.Contains(threshold);
        }

        /// <summary>获取所有已触达的里程碑阈值（只读）</summary>
        public IReadOnlyCollection<int> GetReachedMilestones()
        {
            return this.reachedMilestones;
        }

        /// <summary>重置里程碑追踪状态（不影响 GameplaySessionStats 中的历史数据）</summary>
        public void ResetMilestones()
        {
            this.reachedMilestones.Clear();
            this.totalCollected = 0;
            this.GameLogger.Log("[ItemCollectionTracker] 里程碑追踪已重置");
        }

        /// <summary>遍历阈值列表，检查是否触发新的里程碑</summary>
        private void CheckMilestones()
        {
            foreach (int threshold in MilestoneThresholds)
            {
                if (this.totalCollected >= threshold && !this.reachedMilestones.Contains(threshold))
                {
                    this.reachedMilestones.Add(threshold);
                    this.FireMilestone(threshold);
                }
            }
        }

        /// <summary>触达里程碑：发送 Tip 提示 + 触发事件 + 写日志</summary>
        private void FireMilestone(int threshold)
        {
            string tipText = string.Format("收集里程碑达成: 已收集 {0} 个物品!", threshold);

            // 通过 Tip 系统给予即时反馈；降级保护：Tip 预制体缺失时不崩溃
            try
            {
                AWorkerTask.ShowTipProvider(tipText);
            }
            catch (Exception ex)
            {
                AWorkerTask.LogProvider(
                    $"ItemCollectionTracker.FireMilestone ShowTip failed, threshold: {threshold}.\n{ex}",
                    LogManager.LogLevelEnum.Error);
            }

            this.GameLogger.Log(string.Format(
                "[ItemCollectionTracker] 里程碑触达: {0} 个物品 (累计 {1})", threshold, this.totalCollected));

            this.MilestoneReached?.Invoke(threshold, this.totalCollected);
        }
    }
}
