namespace LAB2D.Item.Build
{
    /// <summary>
    /// 火把 — 单格发光建筑：建成自带暖色点光源（BuildItemData.LightRadius=2.5，
    /// Additive + 平滑闪烁），夜间照亮道路/防线。光源经 BuildMap.GetBuildLightConfig →
    /// TileVisualSpawner.SyncLight 挂接，建造中不发光、读档幂等重建；本类无需任何代码
    /// （三同约定：类名 == 瓦片名 == SO 条目名，帧动画 Torch_0..3 走 IsAnimation）。
    /// </summary>
    public class Torch : ABuildItem
    {
    }
}
