namespace LAB2D.Domain.Gameplay.GongFa
{
    using LAB2D.Domain.Character.Growth;
    using LAB2D.Domain.Gameplay.Cultivation;

    /// <summary>
    /// 武学功法定义 — 内功心法（被动加成+回蓝）与外功招式（主动技能）共用模板。
    /// 纯 C# 数据类，静态库 <see cref="GongFaLibrary"/> 提供实例。
    /// </summary>
    public class GongFaDef
    {
        /// <summary>功法唯一标识（存档持久化键）。</summary>
        public string Id;

        /// <summary>功法显示名称。</summary>
        public string Name;

        /// <summary>功法描述文本。</summary>
        public string Description;

        /// <summary>功法五行属性（影响与灵根匹配的修炼速度加成）。</summary>
        public Element Element;

        /// <summary>是否内功心法（true=被动加成，false=外功招式注册为主动技能）。</summary>
        public bool IsNeiGong;

        /// <summary>内功被动加成（仅 IsNeiGong 时生效，走统一成长收集管线）。</summary>
        public GrowthBonus Bonus;

        /// <summary>内功回蓝速率（点/秒，仅 IsNeiGong 时生效，激活后由 GongFaManager 消费）。</summary>
        public float MpRegenPerSec;

        /// <summary>修炼所需境界索引（0=凡人，见 RealmLibrary）。</summary>
        public int RequiredRealmIndex;

        /// <summary>外功招式关联的技能 Id（仅非内功时有值，对应 SkillConstant）。</summary>
        public string SkillId;

        /// <summary>外功招式的 HUD 槽位分配顺序（越小越先占用靠前槽位；仅非内功时有意义）。</summary>
        public int SkillOrder;
    }
}
