namespace LAB2D.Gameplay
{
    using LAB2D;
    using LAB2D.Domain.Gameplay;
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// 会话结束统计数据模型 — 结构化保存一次游戏会话的完整结算数据。
    /// 数据来源：GameplaySessionStats.CreateSnapshot()
    /// 用途：结算面板数据源、关卡评价展示、玩家行为分析、后续成就/任务条件判断
    ///
    /// 接入方式：
    ///   1. 通过 SessionResultManager.CaptureResult() 从当前 GameplaySessionStats 快照生成
    ///   2. 可直接序列化为 JSON 用于存档或上报
    ///   3. GetReportText() 生成格式化文本报告，用于 Debug 或 Editor 展示
    /// </summary>
    [Serializable]
    public class SessionResultData
    {
        // ---- 基础信息 ----

        /// <summary>会话时长（秒）</summary>
        public float SessionDuration;

        /// <summary>数据采集时间戳</summary>
        public string CapturedAt;

        // ---- 战斗数据 ----

        /// <summary>总造成伤害</summary>
        public int TotalDamageDealt;

        /// <summary>总承受伤害</summary>
        public int TotalDamageTaken;

        /// <summary>总敌人数击杀</summary>
        public int TotalDefeatedEnemyCount;

        /// <summary>最高连击数</summary>
        public int MaxCombo;

        /// <summary>暴击次数</summary>
        public int CriticalHitCount;

        /// <summary>暴击率（百分比 0-100）</summary>
        public float CriticalHitRate;

        // ---- 生存数据 ----

        /// <summary>玩家死亡次数</summary>
        public int PlayerDeathCount;

        /// <summary>工人死亡次数</summary>
        public int TotalWorkerDeathCount;

        // ---- 经济数据 ----

        /// <summary>总经验获取</summary>
        public int TotalExperienceGained;

        /// <summary>总物品收集数</summary>
        public int TotalCollectedItemCount;

        /// <summary>工人任务完成数</summary>
        public int TotalWorkerTaskCompletedCount;

        // ---- 衍生计算数据 ----

        /// <summary>伤害效率（输出/承受比，承受为0时取输出值）</summary>
        public float DamageEfficiency;

        /// <summary>战斗评分（0-10000），综合多维度加权计算</summary>
        public int CombatScore;

        /// <summary>星级评价（1-5）</summary>
        public int StarRating;

        /// <summary>评级文本（S/A/B/C/D）</summary>
        public string GradeText;

        /// <summary>存活状态（玩家死亡次数为0视为存活通关）</summary>
        public bool HasSurvived;

        private static readonly SessionResultRuleService RuleService = new SessionResultRuleService();

        // ---- 详细统计 ----

        /// <summary>按敌人类型分组的击杀统计</summary>
        public Dictionary<string, int> DefeatedEnemiesByType;

        /// <summary>按任务类型分组的工人任务完成统计</summary>
        public Dictionary<string, int> CompletedWorkerTasksByType;

        /// <summary>
        /// 从 GameplaySessionStatsSnapshot 创建结算数据，并自动计算评分和评级。
        /// 调用时机：会话结束时（玩家死亡、波次通关、手动触发）
        /// </summary>
        /// <param name="snapshot">GameplaySessionStats 快照</param>
        /// <returns>计算完成的结算数据</returns>
        public static SessionResultData FromSnapshot(GameplaySessionStatsSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return null;
            }

            SessionResultData result = new SessionResultData
            {
                CapturedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                SessionDuration = snapshot.SessionDuration,
                TotalDamageDealt = snapshot.TotalDamageDealt,
                TotalDamageTaken = snapshot.TotalDamageTaken,
                TotalDefeatedEnemyCount = snapshot.TotalDefeatedEnemyCount,
                MaxCombo = snapshot.MaxCombo,
                CriticalHitCount = snapshot.CriticalHitCount,
                PlayerDeathCount = snapshot.PlayerDeathCount,
                TotalWorkerDeathCount = snapshot.TotalWorkerDeathCount,
                TotalExperienceGained = snapshot.TotalExperienceGained,
                TotalCollectedItemCount = snapshot.TotalCollectedItemCount,
                TotalWorkerTaskCompletedCount = snapshot.TotalWorkerTaskCompletedCount,
                HasSurvived = snapshot.PlayerDeathCount == 0,
                DefeatedEnemiesByType = snapshot.DefeatedEnemiesByType != null
                    ? new Dictionary<string, int>(snapshot.DefeatedEnemiesByType)
                    : new Dictionary<string, int>(),
                CompletedWorkerTasksByType = snapshot.CompletedWorkerTasksByType != null
                    ? new Dictionary<string, int>(snapshot.CompletedWorkerTasksByType)
                    : new Dictionary<string, int>(),
            };

            result.CalculateDerivedStats();
            return result;
        }

        /// <summary>
        /// 计算衍生数据：暴击率、伤害效率、战斗评分、星级和评级。
        /// 评分权重分配：
        ///   击杀分 35%：基于击杀数量（每只 100 分，上限 3500）
        ///   连击分 25%：基于最高连击（每次 50 分，上限 2500）
        ///   生存分 20%：存活通关 +2000，每次死亡扣 500
        ///   效率分 15%：基于伤害效率比
        ///   收集分 5%：  基于物品收集数
        /// </summary>
        private void CalculateDerivedStats()
        {
            this.CriticalHitRate = RuleService.CalculateCriticalHitRate(
                this.CriticalHitCount,
                this.TotalDamageDealt);

            this.DamageEfficiency = RuleService.CalculateDamageEfficiency(
                this.TotalDamageDealt,
                this.TotalDamageTaken);

            this.CombatScore = RuleService.CalculateCombatScore(
                this.TotalDefeatedEnemyCount,
                this.MaxCombo,
                this.HasSurvived,
                this.PlayerDeathCount,
                this.DamageEfficiency,
                this.TotalCollectedItemCount);

            this.StarRating = RuleService.GetStarRating(this.CombatScore);
            this.GradeText = RuleService.GetGradeText(this.CombatScore);
        }

        /// <summary>
        /// 生成格式化文本报告，用于 Debug 输出、Editor 弹窗或日志记录。
        /// </summary>
        /// <returns>格式化文本</returns>
        public string GetReportText()
        {
            StringBuilder builder = new StringBuilder(512);
            builder.AppendLine("╔══════════════════════════════╗");
            builder.AppendLine("║     关卡结算报告             ║");
            builder.AppendLine("╚══════════════════════════════╝");
            builder.AppendLine();
            builder.AppendLine($"采集时间：{this.CapturedAt}");
            builder.AppendLine($"会话时长：{this.SessionDuration:0.0} 秒");
            builder.AppendLine();
            builder.AppendLine("【综合评价】");
            builder.AppendLine($"  战斗评分：{this.CombatScore} / 10000");
            builder.AppendLine($"  星级评价：{new string('★', this.StarRating)}{new string('☆', 5 - this.StarRating)} ({this.StarRating}/5)");
            builder.AppendLine($"  等级评价：{this.GradeText}");
            builder.AppendLine($"  存活通关：{(this.HasSurvived ? "是" : "否")}");
            builder.AppendLine();
            builder.AppendLine("【战斗数据】");
            builder.AppendLine($"  击杀敌人：{this.TotalDefeatedEnemyCount}");
            builder.AppendLine($"  最高连击：{this.MaxCombo}");
            builder.AppendLine($"  造成伤害：{this.TotalDamageDealt}");
            builder.AppendLine($"  承受伤害：{this.TotalDamageTaken}");
            builder.AppendLine($"  伤害效率：{this.DamageEfficiency:0.0}x");
            builder.AppendLine($"  暴击次数：{this.CriticalHitCount}（暴击率 {this.CriticalHitRate:0.0}%）");
            builder.AppendLine();
            builder.AppendLine("【生存数据】");
            builder.AppendLine($"  玩家死亡：{this.PlayerDeathCount} 次");
            builder.AppendLine($"  工人死亡：{this.TotalWorkerDeathCount} 次");
            builder.AppendLine();
            builder.AppendLine("【经济数据】");
            builder.AppendLine($"  经验获取：{this.TotalExperienceGained}");
            builder.AppendLine($"  物品收集：{this.TotalCollectedItemCount}");
            builder.AppendLine($"  工人任务：{this.TotalWorkerTaskCompletedCount}");

            if (this.DefeatedEnemiesByType != null && this.DefeatedEnemiesByType.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("【击杀明细】");
                foreach (KeyValuePair<string, int> kvp in this.DefeatedEnemiesByType)
                {
                    builder.AppendLine($"  {kvp.Key}：{kvp.Value} 只");
                }
            }

            if (this.CompletedWorkerTasksByType != null && this.CompletedWorkerTasksByType.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("【任务完成明细】");
                foreach (KeyValuePair<string, int> kvp in this.CompletedWorkerTasksByType)
                {
                    builder.AppendLine($"  {kvp.Key}：{kvp.Value} 次");
                }
            }

            builder.AppendLine();
            builder.AppendLine("════════════════════════════════");
            return builder.ToString();
        }
    }
}
