namespace LAB2D.Domain.Gameplay.AwakenedPower
{
    using LAB2D.Domain.Character.Growth;

    /// <summary>
    /// 异能定义 — 觉醒后赋予玩家一个主动技能（复用 SkillManager 扩展槽位）。
    /// 纯 C# 数据类，静态库 <see cref="AwakenedPowerLibrary"/> 提供实例。
    /// </summary>
    public class AwakenedPowerDef
    {
        /// <summary>异能唯一标识（GrowthData.AwakenedPowerIds 持久化键）。</summary>
        public string Id;

        /// <summary>异能显示名称。</summary>
        public string Name;

        /// <summary>异能描述文本。</summary>
        public string Description;

        /// <summary>觉醒后注册的主动技能 Id（对应 SkillConstant）。</summary>
        public string SkillId;

        /// <summary>
        /// Worker 觉醒被动加成 — Worker 无技能栏（SkillManager 槽位为玩家全局共享，
        /// 注册会挤占玩家槽位），觉醒时把该加成入账 GrowthData.PermanentRealmBonus
        /// 走统一属性管线；玩家觉醒不走此字段（拿主动技能）。
        /// </summary>
        public GrowthBonus WorkerPassiveBonus;
    }
}
