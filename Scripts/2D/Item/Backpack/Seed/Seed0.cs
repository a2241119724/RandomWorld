namespace LAB2D.Item.Backpack.Seed
{
    /// <summary>
    /// 种子（旧多条目时代的具体类，SO 已合并为单条 Seed 后无对应条目）。
    /// abstract 使反射扫描（GetChildByParent 过滤抽象类）跳过本类——
    /// 否则 InitItemInstances 兜底把它注册进背包，启动填充时 GetByName("Seed0")
    /// 查不到 SO 条目，报 Warning 且生成 id=0 幽灵物品（2026-09-04 日志实锤）。
    /// 种植系统按 ItemType==Seed 判断，不依赖类名；恢复多条目时去掉 abstract 即可。
    /// </summary>
    public abstract class Seed0 : ASeed
    {
    }
}
