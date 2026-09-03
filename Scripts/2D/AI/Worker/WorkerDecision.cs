namespace LAB2D.AI.Worker
{
    using LAB2D.Data;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Worker 行为决策类型。
    /// </summary>
    public enum WorkerDecisionType
    {
        /// <summary>什么都不做，等待</summary>
        Idle,

        /// <summary>自己采集资源</summary>
        SelfGather,

        /// <summary>发布悬赏让别人采集</summary>
        PostBounty,

        /// <summary>接受已有悬赏</summary>
        AcceptBounty,

        /// <summary>吃饭恢复饥饿</summary>
        Eat,

        /// <summary>睡觉恢复疲劳</summary>
        Sleep,

        /// <summary>自己去捡地上属于自己的物品</summary>
        SelfCarry,

        /// <summary>去任务栏取回属于自己的悬赏物品</summary>
        PickUp,

        /// <summary>自己去建造</summary>
        SelfBuild,

        /// <summary>自己去种植</summary>
        SelfPlant,

        /// <summary>漫游休息 — 恢复精气神和心情</summary>
        Wander,

        /// <summary>地面睡眠 — 无床时的低效睡眠</summary>
        GroundSleep,

        /// <summary>回家把身上多余物品存入个人四格仓库</summary>
        Store,

        /// <summary>回家从个人四格仓库取任务材料</summary>
        Withdraw,
    }

    /// <summary>
    /// 决策结果 — 包含决策类型和可选的候选位置/资源。
    /// </summary>
    public struct Decision
    {
        public WorkerDecisionType Type;
        public Vector3Int TargetPosition;
        public ResourceInfo Resource;
        public string Description;

        /// <summary>建造任务需要的材料清单（仅 Build 类型使用）</summary>
        public Dictionary<int, ResourceInfo> NeededResources;

        /// <summary>建造物品的 Tile 名称（仅 Build 类型使用）</summary>
        public string BuildTileName;

        /// <summary>是否为地形挖掘（而非资源采集）。</summary>
        public bool IsTerrainDig;

        /// <summary>要挖掘的地形 ID（仅 IsTerrainDig=true 时有效）。</summary>
        public int TerrainId;

        /// <summary>Withdraw 决策要取的物料（id→数量，仅 Withdraw 类型使用）。</summary>
        public Dictionary<int, int> WithdrawNeeds;

        public static Decision Make(WorkerDecisionType type, string desc = "")
        {
            return new Decision { Type = type, Description = desc };
        }

        public static Decision MakeGather(Vector3Int pos, ResourceInfo resource, string desc = "")
        {
            return new Decision
            {
                Type = WorkerDecisionType.SelfGather,
                TargetPosition = pos,
                Resource = resource,
                Description = desc,
            };
        }

        /// <summary>创建建造决策（自己建造或发悬赏建造）。</summary>
        public static Decision MakeBuild(WorkerDecisionType type, Vector3Int buildPos, string tileName,
            Dictionary<int, ResourceInfo> needs, string desc = "")
        {
            return new Decision
            {
                Type = type,
                TargetPosition = buildPos,
                BuildTileName = tileName,
                NeededResources = needs,
                Description = desc,
            };
        }

        /// <summary>创建种植决策。</summary>
        public static Decision MakePlant(WorkerDecisionType type, Vector3Int farmlandPos, string desc = "")
        {
            return new Decision
            {
                Type = type,
                TargetPosition = farmlandPos,
                Description = desc,
            };
        }

        /// <summary>创建"回家从个人仓库取料"决策。</summary>
        public static Decision MakeWithdraw(Vector3Int tile, Dictionary<int, int> needs, string desc = "")
        {
            return new Decision
            {
                Type = WorkerDecisionType.Withdraw,
                TargetPosition = tile,
                WithdrawNeeds = needs,
                Description = desc,
            };
        }
    }
}
