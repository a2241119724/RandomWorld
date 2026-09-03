namespace LAB2D.Item.Build
{
    /// <summary>
    /// 箭塔 —— 自动索敌射击的防御建筑（M2B）。
    /// 类名==瓦片名==SO 条目名（BuildOtherItemData：Id 1100004、IsNeedBuild=1、IsPass=0），
    /// ItemInstanceFactory 反射自动注册；玩家建造走 ABuildItem 通用管线（1×1 无副格），
    /// 建成后主格阻挡可被打。射击逻辑在 ArrowTowerManager（ITickable 扫描已建成塔）。
    /// </summary>
    public class ArrowTower : ABuildItem
    {
    }
}
