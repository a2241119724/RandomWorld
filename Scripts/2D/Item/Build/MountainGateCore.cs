namespace LAB2D.Item.Build
{
    using LAB2D;
    using System;
    using UnityEngine;

    /// <summary>
    /// 山门核心（3×3）—— 系统放置建筑，小镇胜负锚点（M1.3）。
    /// 类名==瓦片名==SO 条目名（BuildOtherItemData：IsNeedBuild=false、IsPass=false），
    /// ItemInstanceFactory 反射自动注册；放置走既有建造管线
    /// （AddBuildTask → 主格 AddBuild + 副格 RegisterCollisionTile 阻挡）。
    /// 玩家建造入口在此拦截；MountainGateManager 经 PlaceBySystem 系统放置。
    /// </summary>
    public class MountainGateCore : ABuildItem
    {
        public MountainGateCore()
        {
            this.Width = Gameplay.MountainGateManager.CoreSize;
            this.Height = Gameplay.MountainGateManager.CoreSize;
            this.RectType = AWorkerTask.RectType.BottomLeft; // 床等多格惯例：参考点即主格
        }

        /// <summary>
        /// 玩家不可建造：山门核心由小镇系统放置。
        /// </summary>
        public override void AddBuildTask(Vector3Int centerMap, Extra extra, int priority = WorkerTaskPriority.SystemDefault)
        {
            AWorkerTask.LogProvider(
                "[BuildDiag] 建造被拦截：山门核心由系统放置，不可人工建造",
                LogManager.LogLevelEnum.Debug);
            try
            {
                Core.GameServices.ShowTipProvider("山门核心由小镇自行生长，无法人工建造");
            }
            catch (Exception)
            {
                // Tip 不可用时静默降级（测试环境）
            }
        }

        /// <summary>
        /// 系统放置入口（MountainGateManager 调用）：绕过玩家拦截，走既有建造管线。
        /// SO 条目 IsNeedBuild=false → 主格放置即完成（无需建造任务），
        /// IsPass=false → 主格物理阻挡可被打、副格 A* 阻挡（IsCanReach 通用逻辑）。
        /// </summary>
        public void PlaceBySystem(Vector3Int centerMap)
        {
            base.AddBuildTask(centerMap, null);
        }
    }
}
