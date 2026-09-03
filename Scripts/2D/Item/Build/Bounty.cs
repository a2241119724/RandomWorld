namespace LAB2D.Item.Build
{
    using LAB2D;
    using System;
    using UnityEngine;

    /// <summary>
    /// 悬赏牌（任务发布栏图标，1×1）—— 系统放置建筑。
    /// 类名==瓦片名==SO 条目名（BuildOtherItemData：IsNeedBuild=false、IsPass=false），
    /// ItemInstanceFactory 反射自动注册；放置走既有建造管线
    /// （AddBuildTask → AddBuild 主格放置即完成 + 碰撞按 IsPass）。
    /// 玩家建造入口在此拦截；TaskBoardManager 经 PlaceBySystem 系统放置。
    /// </summary>
    public class Bounty : ABuildItem
    {
        /// <summary>
        /// 玩家不可建造：悬赏牌由任务栏系统放置。
        /// </summary>
        public override void AddBuildTask(Vector3Int centerMap, Extra extra, int priority = WorkerTaskPriority.SystemDefault)
        {
            AWorkerTask.LogProvider(
                "[BuildDiag] 建造被拦截：悬赏牌由系统放置，不可人工建造",
                LogManager.LogLevelEnum.Debug);
            try
            {
                Core.GameServices.ShowTipProvider("悬赏牌由小镇自行设立，无法人工建造");
            }
            catch (Exception)
            {
                // Tip 不可用时静默降级（测试环境）
            }
        }

        /// <summary>
        /// 系统放置入口（TaskBoardManager 调用）：绕过玩家拦截，走既有建造管线。
        /// SO 条目 IsNeedBuild=false → 放置即完成（无需建造任务，读档重放幂等），
        /// IsPass=false → 物理阻挡 + A* 不可达（IsCanReach 双真值一致）。
        /// </summary>
        public void PlaceBySystem(Vector3Int centerMap)
        {
            base.AddBuildTask(centerMap, null);
        }
    }
}
