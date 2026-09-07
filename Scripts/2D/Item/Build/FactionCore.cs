namespace LAB2D.Item.Build
{
    using LAB2D;
    using System;
    using UnityEngine;

    /// <summary>
    /// 阵营核心（3×3）—— 系统放置建筑，Worker 阵营的守卫锚点（P1）。
    /// 类名==瓦片名==SO 条目名（BuildOtherItemData：IsNeedBuild=true、IsPass=false），
    /// ItemInstanceFactory 反射自动注册；放置走既有建造管线
    /// （AddBuildTask → 主格 AddBuild 创建建造任务 + 副格 RegisterCollisionTile 阻挡）。
    /// 玩家建造入口在此拦截；FactionManager 经 PlaceBySystem 系统放置（读档重放幂等）。
    /// </summary>
    public class FactionCore : ABuildItem
    {
        public FactionCore()
        {
            this.Width = Gameplay.FactionManager.CoreSize;
            this.Height = Gameplay.FactionManager.CoreSize;
            this.RectType = AWorkerTask.RectType.BottomLeft; // 床等多格惯例：参考点即主格
        }

        /// <summary>
        /// 玩家不可建造：阵营核心由阵营系统放置（玩家创阵营走 FactionManager.TryCreateFactionByPlayer）。
        /// </summary>
        public override void AddBuildTask(Vector3Int centerMap, Extra extra, int priority = WorkerTaskPriority.SystemDefault)
        {
            AWorkerTask.LogProvider(
                "[BuildDiag] 建造被拦截：阵营核心由阵营系统放置，不可人工建造",
                LogManager.LogLevelEnum.Debug);
            try
            {
                Core.GameServices.ShowTipProvider("阵营核心随阵营而立，无法人工建造");
            }
            catch (Exception)
            {
                // Tip 不可用时静默降级（测试环境）
            }
        }

        /// <summary>
        /// 系统放置入口（FactionManager 调用）：绕过玩家拦截，走既有建造管线。
        /// SO 条目 IsNeedBuild=true → 放置后创建建造任务，Worker 参与建核心；
        /// IsPass=false → 建成后主格物理阻挡可被打、副格 A* 阻挡（IsCanReach 通用逻辑）。
        /// </summary>
        public void PlaceBySystem(Vector3Int centerMap)
        {
            base.AddBuildTask(centerMap, null);
        }
    }
}
