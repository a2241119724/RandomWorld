namespace LAB2D.Item.Build
{
    /// <summary>
    /// 篝火 — 单格强发光建筑：建成自带大半径暖光（BuildItemData.LightRadius=4.0，
    /// Additive + 平滑闪烁），夜间聚集点/营地核心照明。光源链路同 Torch
    /// （BuildMap.GetBuildLightConfig → TileVisualSpawner.SyncLight，建造中不发光、
    /// 读档幂等重建）；三同约定零代码（帧动画 Campfire_0..3 走 IsAnimation）。
    /// </summary>
    public class Campfire : ABuildItem
    {
    }
}
