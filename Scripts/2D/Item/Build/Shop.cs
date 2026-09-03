namespace LAB2D.Item.Build
{
    using LAB2D;
    using System;
    using UnityEngine;

    /// <summary>
    /// 商店图标（1×1）—— 系统放置建筑（ShopNPC 预制体另由 ShopNPCGenerator 实例化）。
    /// 类名==瓦片名==SO 条目名（BuildOtherItemData：IsNeedBuild=false、IsPass=false），
    /// ItemInstanceFactory 反射自动注册；放置走既有建造管线
    /// （AddBuildTask → AddBuild 主格放置即完成 + 碰撞按 IsPass）。
    /// 玩家建造入口在此拦截；ShopNPCGenerator 经 PlaceBySystem 系统放置。
    /// </summary>
    public class Shop : ABuildItem
    {
        /// <summary>
        /// 玩家不可建造：商店由系统生成。
        /// </summary>
        public override void AddBuildTask(Vector3Int centerMap, Extra extra, int priority = WorkerTaskPriority.SystemDefault)
        {
            AWorkerTask.LogProvider(
                "[BuildDiag] 建造被拦截：商店由系统生成，不可人工建造",
                LogManager.LogLevelEnum.Debug);
            try
            {
                Core.GameServices.ShowTipProvider("商店由集市自行开张，无法人工建造");
            }
            catch (Exception)
            {
                // Tip 不可用时静默降级（测试环境）
            }
        }

        /// <summary>
        /// 系统放置入口（ShopNPCGenerator 调用）：绕过玩家拦截，走既有建造管线。
        /// AddBuild 内部先登记 PosMap 再 visuals.CreateOrUpdate，天然规避
        /// 旧手写路径「PosMap 无条目回退 Alpha 淡化模式」的顺序 bug。
        /// SO 条目 IsNeedBuild=false → 放置即完成；IsPass=false → 物理阻挡
        /// （与 ShopNPC 预制体 Collider2D 双保险）+ A* 不可达。
        /// </summary>
        public void PlaceBySystem(Vector3Int centerMap)
        {
            base.AddBuildTask(centerMap, null);
        }
    }
}
