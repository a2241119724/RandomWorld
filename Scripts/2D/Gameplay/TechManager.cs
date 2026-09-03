namespace LAB2D.Gameplay
{
    using System;
    using System.Collections.Generic;
    using LAB2D.Domain.Tech;
    using LAB2D.Serializable;
    using UnityEngine;

    /// <summary>
    /// 科技管理器 — 研究点产出（已建成研究台按时间产出）、科技研究结算、
    /// 建筑解锁 gating 与科技加成查询（农耕/打坐/研究产出）。
    /// 存档经 ASingletonSaveData 由 ArchiveManager 反射自动发现。
    /// 单例，由 GlobalInit 注册并驱动（ITickable）。
    /// </summary>
    public class TechManager : ASingletonSaveData<TechManager>, ITickable
    {
        /// <summary>每座研究台产出 1 研究点所需秒数（1 点/分/台）。</summary>
        internal const float SecondsPerPointPerTable = 60f;

        /// <summary>扫描已建成建筑的节流间隔（秒）。</summary>
        private const float BuildingScanInterval = 2f;

        internal static Func<BuildMap> BuildMapProvider { get; set; }
            = () => ServiceLocator.TryGet(out BuildMap bm) ? bm : null;

        internal static Action<string> TipProvider { get; set; }
            = (msg) =>
            {
                try
                {
                    Core.GameServices.ShowTipProvider(msg);
                }
                catch (Exception)
                {
                    // Tip 不可用时静默降级（初始化早期/测试环境）
                }
            };

        /// <summary>当前研究点。</summary>
        public float ResearchPoints { get; set; }

        /// <summary>已研究的科技 Id 列表。</summary>
        public List<string> ResearchedIds { get; set; }

        private float researchCarry;
        private float buildingScanTimer;
        private int researchTableCount;
        private int spiritArrayCount;

        public TechManager()
        {
            this.ResearchedIds = new List<string>();
        }

        /// <summary>
        /// 读档兜底：BinaryFormatter 不跑构造函数，旧档/缺字段时 ResearchedIds 可能为 null，幂等补建。
        /// </summary>
        public void Ensure()
        {
            if (this.ResearchedIds == null)
            {
                this.ResearchedIds = new List<string>();
            }
        }

        /// <summary>
        /// 研究点产出：每座已建成研究台按时间积累（研究点为 float，面板显示取整）。
        /// 高级研究法已研究时产出 ×2。
        /// </summary>
        public void Tick(float deltaTime)
        {
            this.buildingScanTimer += deltaTime;
            if (this.buildingScanTimer >= BuildingScanInterval)
            {
                this.buildingScanTimer = 0f;
                this.RescanBuildings();
            }

            if (this.researchTableCount <= 0)
            {
                return;
            }

            float mul = 1f + this.GetResearchSpeedBonus();
            this.researchCarry += deltaTime * this.researchTableCount * mul / SecondsPerPointPerTable;
            if (this.researchCarry >= 1f)
            {
                int add = (int)this.researchCarry;
                this.researchCarry -= add;
                this.ResearchPoints += add;
            }
        }

        /// <summary>
        /// 尝试研究科技：点数足够且未研究时扣除并记录。
        /// </summary>
        /// <param name="techId">科技 Id。</param>
        /// <returns>是否研究成功。</returns>
        public bool Research(string techId)
        {
            this.Ensure();
            TechDef def = TechLibrary.Get(techId);
            if (def == null)
            {
                return false;
            }

            if (!TechRuleService.CanResearch(this.IsResearched(techId), this.ResearchPoints, def))
            {
                TipProvider($"研究失败：{def.Name} 需 {def.Cost:F0} 研究点");
                return false;
            }

            this.ResearchPoints -= def.Cost;
            this.ResearchedIds.Add(techId);
            TipProvider($"研究完成：{def.Name}");
            AWorkerTask.LogProvider(
                $"[TechDiag] 研究 {def.Name}({techId}) 完成，剩余研究点 {this.ResearchPoints:F0}",
                LogManager.LogLevelEnum.Debug);
            return true;
        }

        /// <summary>是否已研究指定科技。</summary>
        public bool IsResearched(string techId)
        {
            return this.ResearchedIds != null && this.ResearchedIds.Contains(techId);
        }

        /// <summary>
        /// 建筑是否可建造：不在任何科技解锁名单中的建筑默认可建；
        /// 在名单中的（当前仅聚灵阵）需已研究对应科技。
        /// </summary>
        /// <param name="tileName">建筑瓦片名（ABuildItem 类名）。</param>
        /// <returns>是否可建造。</returns>
        public bool IsBuildUnlocked(string tileName)
        {
            if (string.IsNullOrEmpty(tileName))
            {
                return true;
            }

            foreach (TechDef def in TechLibrary.All)
            {
                if (def.UnlockBuildName == tileName)
                {
                    return this.IsResearched(def.Id);
                }
            }

            return true;
        }

        /// <summary>农耕速度加成（灵耕术，加数 0.25 = +25%）。</summary>
        public float GetFarmSpeedBonus()
        {
            return TechRuleService.SumBonus(this.ResearchedIds, t => t.FarmSpeedBonus);
        }

        /// <summary>
        /// 打坐灵气积累全局加成 — M4 起聚灵阵科技只解锁建造（加数已归 0，恒返 0），
        /// 局部加成由 LingQiManager 按空间浓度提供。管线保留待未来全局科技。
        /// </summary>
        public float GetMeditateSpeedBonus()
        {
            return TechRuleService.SumBonus(this.ResearchedIds, t => t.MeditateSpeedBonus);
        }

        /// <summary>研究点产出倍率加成（高级研究法，加数 1.0 = ×2）。</summary>
        public float GetResearchSpeedBonus()
        {
            return TechRuleService.SumBonus(this.ResearchedIds, t => t.ResearchSpeedBonus);
        }

        /// <summary>已建成研究台数量（缓存值，Tick 节流刷新）。</summary>
        public int ResearchTableCount => this.researchTableCount;

        /// <summary>
        /// 立即重扫已建成建筑数量（面板刷新/Tick 节流共用）。
        /// </summary>
        public void RescanBuildings()
        {
            this.researchTableCount = this.CountCompletedBuildings("ResearchTable");
            this.spiritArrayCount = this.CountCompletedBuildings("SpiritArray");
        }

        /// <summary>已建成聚灵阵数量（缓存值，Tick 节流刷新）。</summary>
        public int SpiritArrayCount => this.spiritArrayCount;

        /// <summary>
        /// 统计 BuildMap 中指定名称且已建造完成的建筑格数（多格建筑每格都会计入，
        /// 仅用于"有无/加成不叠乘"判定，无碍）。
        /// </summary>
        private int CountCompletedBuildings(string tileName)
        {
            BuildMap buildMap = BuildMapProvider();
            if (buildMap?.BuildMapDataLAB?.PosMap == null)
            {
                return 0;
            }

            int count = 0;
            foreach (KeyValuePair<Vector3IntLAB, BuildMap.BuildTileData> kv in buildMap.BuildMapDataLAB.PosMap)
            {
                BuildMap.BuildTileData tile = kv.Value;
                if (tile != null && tile.IsComplete && tile.Name == tileName)
                {
                    count++;
                }
            }

            return count;
        }

        /// <inheritdoc/>
        public override void SaveData()
        {
            base.SaveData();
            this.Ensure();
            TechManagerData data = new TechManagerData
            {
                ResearchPoints = this.ResearchPoints,
                ResearchedIds = this.ResearchedIds,
            };
            DataTool.SaveDataByBinary(GlobalData.ConfigFile.GetPath(this.GetType().Name), data);
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            base.LoadData();
            TechManagerData data = DataTool.LoadDataByBinary<TechManagerData>(
                GlobalData.ConfigFile.GetPath(this.GetType().Name));
            if (data != null)
            {
                this.ResearchPoints = data.ResearchPoints;
                this.ResearchedIds = data.ResearchedIds;
            }

            this.Ensure();
            this.RescanBuildings();
            AWorkerTask.LogProvider(
                $"[TechDiag] TechManager 读档：研究点 {this.ResearchPoints:F0}，已研究 {this.ResearchedIds.Count} 项",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>
        /// 科技存档数据（BinaryFormatter 直存）。
        /// </summary>
        [Serializable]
        public class TechManagerData
        {
            /// <summary>当前研究点。</summary>
            public float ResearchPoints;

            /// <summary>已研究的科技 Id 列表。</summary>
            public List<string> ResearchedIds;
        }
    }
}
