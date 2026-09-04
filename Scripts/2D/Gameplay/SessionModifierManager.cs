namespace LAB2D.Gameplay
{
    using LAB2D.Domain.Gameplay;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 每局修饰符管理器 — 开局 roll 2~3 个全局修饰符（灵气/敌方强度/工作速度/战利品通道），
    /// 整局生效、入档随存档恢复、与事件天气正交叠乘（docs/游戏大纲.md 包 4「每局不一样」）。
    /// 仿 FavorabilityManager：ASingletonSaveData 单例 + 二进制存档；规则收口在 Domain 层
    /// SessionModifierRuleService（确定性 Roll，seed 入档可溯源）。
    /// 初始化时序：ArchiveManager LoadData（有档恢复 / 无档重 roll）→ GlobalInit Initialize（空则兜底 roll）。
    /// </summary>
    public class SessionModifierManager : ASingletonSaveData<SessionModifierManager>, IInitializable
    {
        /// <summary>开局抽取数量上限（下限 2）。</summary>
        private const int MaxRollCount = 3;

        /// <summary>roll 随机源（测试可替换桩）。</summary>
        internal static Func<int> SeedProvider { get; set; }
            = () => UnityEngine.Random.Range(int.MinValue, int.MaxValue);

        /// <summary>第三枚修饰符的触发概率（2~3 枚各半）。</summary>
        internal static Func<float> FloatProvider { get; set; }
            = () => UnityEngine.Random.value;

        /// <summary>开局 Tip 通道（Tip 不可用时静默降级，AwakenedPowerManager 同款）。</summary>
        internal static Action<string> TipProvider { get; set; }
            = (msg) =>
            {
                try
                {
                    Core.GameServices.ShowTipProvider(msg);
                }
                catch (Exception)
                {
                    // Tip 不可用（初始化早期/测试环境）静默降级，HUD (F3) 仍可查
                }
            };

        /// <summary>本局激活的修饰符 id 列表（按池定义序稳定）。</summary>
        private readonly List<string> activeIds = new List<string>();

        private bool isInitialized;

        /// <summary>本局激活的修饰符 id（只读视图，HUD/测试用）。</summary>
        public IReadOnlyList<string> ActiveIds => this.activeIds;

        /// <summary>本局是否已 roll（无修饰符局也视为已 roll）。</summary>
        public bool HasRolled { get; private set; }

        /// <summary>
        /// 查询通道乘数：无修饰符/未初始化/读档前一律 1f（各接入点零防御成本）。
        /// </summary>
        /// <param name="channel">效果通道。</param>
        /// <returns>该通道累乘系数（无命中为 1）。</returns>
        public float GetChannelMultiplier(ModifierChannel channel)
        {
            return SessionModifierRuleService.GetChannelMultiplier(this.activeIds, channel);
        }

        /// <summary>
        /// 开局 roll（无档路径）。已 roll（读档恢复或重复调用）则跳过，幂等。
        /// </summary>
        public void Initialize()
        {
            if (this.isInitialized)
            {
                return;
            }

            this.isInitialized = true;
            if (!this.HasRolled)
            {
                this.RollNewSession();
            }
        }

        /// <summary>
        /// 重 roll 本局修饰符并广播开局 Tip。仅两条路径调用：Initialize 首次、LoadData 无档（新开局）。
        /// </summary>
        private void RollNewSession()
        {
            this.HasRolled = true;
            int count = MaxRollCount - (FloatProvider() < 0.5f ? 1 : 0);
            this.activeIds.Clear();
            this.activeIds.AddRange(SessionModifierRuleService.Roll(SeedProvider(), count));

            string summary = SessionModifierRuleService.FormatSummary(this.activeIds);
            AWorkerTask.LogProvider(
                $"[ModifierDiag] 本局天机 roll 完成：{summary}",
                LogManager.LogLevelEnum.Debug);
            TipProvider($"本局天机：{summary}（H 查看详情）");
        }

        /// <summary>存档数据（ids 与 seed 双存：ids 为准，seed 仅溯源）。</summary>
        [Serializable]
        public class SessionModifierSaveData
        {
            /// <summary>本局激活的修饰符 id 列表。</summary>
            public List<string> ActiveIds = new List<string>();

            /// <summary>本局 roll 种子（溯源用）。</summary>
            public int Seed;
        }

        /// <inheritdoc/>
        public override void SaveData()
        {
            base.SaveData();
            SessionModifierSaveData data = new SessionModifierSaveData
            {
                ActiveIds = new List<string>(this.activeIds),
            };
            DataTool.SaveDataByBinary(GlobalData.ConfigFile.GetPath(this.GetType().Name), data);
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            base.LoadData();
            SessionModifierSaveData data = DataTool.LoadDataByBinary<SessionModifierSaveData>(
                GlobalData.ConfigFile.GetPath(this.GetType().Name));

            if (data?.ActiveIds != null && data.ActiveIds.Count > 0)
            {
                // 有档恢复：只保留池内已知 id（未知条目按无效果丢弃，前向兼容删池项）
                this.activeIds.Clear();
                foreach (string id in data.ActiveIds)
                {
                    if (SessionModifierRuleService.GetById(id) != null)
                    {
                        this.activeIds.Add(id);
                    }
                }

                this.HasRolled = true;
                AWorkerTask.LogProvider(
                    $"[ModifierDiag] 本局天机读档恢复：{SessionModifierRuleService.FormatSummary(this.activeIds)}",
                    LogManager.LogLevelEnum.Debug);
                return;
            }

            // 无档（新开局/老档迁移）：清残留并重 roll。Initialize 若已跑过（本次 LoadData 由
            // 重开局触发）也需要新的一局，故这里直接重 roll 而不仅清空。
            this.RollNewSession();
        }
    }
}
