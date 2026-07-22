namespace LAB2D.Gameplay
{
    using LAB2D;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 会话结算管理器 — 负责在游戏会话结束时采集、存储和管理结算数据。
    /// 不修改 Scene/Prefab/SO/存档/Photon，仅作为运行时数据层使用。
    ///
    /// 接入方式：
    ///   1. 手动采集：SessionResultManager.Instance.CaptureResult()
    ///   2. 事件触发：GameplaySessionStats.StatsChanged 事件可用于自动检测会话结束
    ///   3. Editor 菜单：工具 > 结算结果 系列菜单
    ///   4. 结算面板数据源：SessionResultManager.Instance.LatestResult
    /// </summary>
    public class SessionResultManager : Singleton<SessionResultManager>
    {
        /// <summary>内存中保留的最大结算历史数</summary>
        private const int MaxHistoryCount = 20;

        /// <summary>结算历史列表（最新在前）</summary>
        private readonly List<SessionResultData> resultHistory;

        /// <summary>
        /// 构造函数：初始化历史列表，订阅 GameplaySessionStats 的数据变更事件
        /// 以便在会话关键节点自动采集结算数据
        /// </summary>
        public SessionResultManager()
        {
            this.resultHistory = new List<SessionResultData>();
        }

        /// <summary>
        /// 最近一次会话结算数据（最新采集结果）
        /// </summary>
        public SessionResultData LatestResult
        {
            get
            {
                return this.resultHistory.Count > 0
                    ? this.resultHistory[0]
                    : null;
            }
        }

        /// <summary>
        /// 会话结算历史数量
        /// </summary>
        public int HistoryCount
        {
            get { return this.resultHistory.Count; }
        }

        /// <summary>
        /// 结算数据变更事件，可用于通知 UI 刷新结算面板
        /// </summary>
        public event Action<SessionResultData> OnResultCaptured;

        /// <summary>
        /// 从当前 GameplaySessionStats 快照采集一次会话结算数据。
        /// 仅在 Play Mode 可用，非 Play Mode 返回 null。
        /// 采集后将结果存入历史列表并触发 OnResultCaptured 事件。
        /// </summary>
        /// <returns>本次采集的结算数据，非 Play Mode 时返回 null</returns>
        public SessionResultData CaptureResult()
        {
            if (!UnityEngine.Application.isPlaying)
            {
                return null;
            }

            GameplaySessionStats stats = Core.ServiceLocator.Get<GameplaySessionStats>();
            if (stats == null)
            {
                return null;
            }

            GameplaySessionStatsSnapshot snapshot = stats.CreateSnapshot();
            if (snapshot == null)
            {
                return null;
            }

            SessionResultData result = SessionResultData.FromSnapshot(snapshot);
            if (result == null)
            {
                return null;
            }

            // 存入历史（最新在前）
            this.resultHistory.Insert(0, result);

            // 超出上限时移除最旧记录
            while (this.resultHistory.Count > MaxHistoryCount)
            {
                this.resultHistory.RemoveAt(this.resultHistory.Count - 1);
            }

            this.OnResultCaptured?.Invoke(result);
            return result;
        }

        /// <summary>
        /// 获取指定索引的结算历史记录（0 为最新）
        /// </summary>
        /// <param name="index">历史索引</param>
        /// <returns>结算数据，索引无效时返回 null</returns>
        public SessionResultData GetResultAt(int index)
        {
            if (index < 0 || index >= this.resultHistory.Count)
            {
                return null;
            }

            return this.resultHistory[index];
        }

        /// <summary>
        /// 获取所有结算历史记录的只读副本
        /// </summary>
        /// <returns>历史列表副本</returns>
        public List<SessionResultData> GetAllResults()
        {
            return new List<SessionResultData>(this.resultHistory);
        }

        /// <summary>
        /// 生成所有历史结算的汇总报告文本
        /// </summary>
        /// <returns>汇总报告文本</returns>
        public string GetHistorySummaryText()
        {
            if (this.resultHistory.Count == 0)
            {
                return "暂无结算记录。\n请在 Play Mode 中使用 工具 > 结算结果 > 立即采集 采集结算数据。";
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder(256);
            builder.AppendLine($"共有 {this.resultHistory.Count} 条结算记录：");
            builder.AppendLine();

            for (int i = 0; i < this.resultHistory.Count; i++)
            {
                SessionResultData r = this.resultHistory[i];
                string marker = i == 0 ? " [最新]" : string.Empty;
                builder.AppendLine(
                    $"#{i + 1}{marker} | {r.CapturedAt} | " +
                    $"评分:{r.CombatScore} | {new string('★', r.StarRating)}{new string('☆', 5 - r.StarRating)} | " +
                    $"击杀:{r.TotalDefeatedEnemyCount} | 连击:{r.MaxCombo} | " +
                    $"{(r.HasSurvived ? "存活" : "死亡" + r.PlayerDeathCount + "次")}");
            }

            return builder.ToString();
        }

        /// <summary>
        /// 清空所有历史结算记录
        /// </summary>
        public void ClearHistory()
        {
            this.resultHistory.Clear();
        }
    }
}
