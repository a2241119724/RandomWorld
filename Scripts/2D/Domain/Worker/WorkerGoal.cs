namespace LAB2D.Domain.Worker
{
    using System.Collections.Generic;

    /// <summary>
    /// Worker 目标类型 — 驱动悬赏和自主行为的核心。
    /// </summary>
    public enum WorkerGoalType
    {
        /// <summary>赚钱攒钱（默认）</summary>
        EarnMoney,

        /// <summary>建造房屋/设施</summary>
        BuildStructure,

        /// <summary>囤积食物</summary>
        StockFood,

        /// <summary>制作装备</summary>
        CraftEquipment,
    }

    /// <summary>
    /// Worker 目标数据 — 描述 Worker 当前想达成什么、缺少什么。
    /// </summary>
    public struct WorkerGoal
    {
        /// <summary>目标类型</summary>
        public WorkerGoalType Type;

        /// <summary>目标描述（调试用）</summary>
        public string Description;

        /// <summary>
        /// 达成目标需要的材料清单（物品ID → 数量）。
        /// 空 = 不需要特定材料（如 EarnMoney）。
        /// </summary>
        public Dictionary<int, int> RequiredMaterials;

        /// <summary>目标优先级（1=最高）</summary>
        public int Priority;

        public static WorkerGoal EarnMoney()
        {
            return new WorkerGoal
            {
                Type = WorkerGoalType.EarnMoney,
                Description = "赚钱攒钱",
                RequiredMaterials = null,
                Priority = 3,
            };
        }

        public static WorkerGoal BuildStructure(string desc, Dictionary<int, int> materials)
        {
            return new WorkerGoal
            {
                Type = WorkerGoalType.BuildStructure,
                Description = desc,
                RequiredMaterials = materials ?? new Dictionary<int, int>(),
                Priority = 1,
            };
        }

        public static WorkerGoal StockFood(int amount = 5)
        {
            return new WorkerGoal
            {
                Type = WorkerGoalType.StockFood,
                Description = $"囤积{amount}份食物",
                RequiredMaterials = null, // 需要扫描食物类物品
                Priority = 2,
            };
        }

        /// <summary>是否有需要收集的材料。</summary>
        public bool HasMaterialNeeds => this.RequiredMaterials != null && this.RequiredMaterials.Count > 0;

        public override string ToString()
        {
            return $"[{this.Type}] {this.Description} (P{this.Priority})";
        }
    }
}
