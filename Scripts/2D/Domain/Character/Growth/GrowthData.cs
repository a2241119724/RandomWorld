namespace LAB2D.Domain.Character.Growth
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 角色成长数据 — 灵根/修仙境界/功法/异能的持久化载体。
    /// 挂在 CharacterData.Growth 字段上，随 CharacterManager 的存档整体序列化。
    /// 注意：BinaryFormatter 反序列化不跑构造函数与字段初始化器，
    /// 读档路径必须先调用 <see cref="Ensure"/> 补建集合字段。
    /// </summary>
    [Serializable]
    public class GrowthData
    {
        /// <summary>五行灵根（Element 枚举值），玩家出生时随机 1~3 条，终身不变；非玩家为空。</summary>
        public List<int> LingGenElements;

        /// <summary>灵根是否已生成。</summary>
        public bool LingGenGenerated;

        /// <summary>当前修仙境界索引（0=凡人，见 RealmLibrary）。</summary>
        public int RealmIndex;

        /// <summary>当前灵气（打坐积累，用于突破）。</summary>
        public float Qi;

        /// <summary>历次境界突破累计的永久加成（Stats 进常规管线，MaxHpFlat 进生命上限）。</summary>
        public GrowthBonus PermanentRealmBonus;

        /// <summary>已学习的功法 Id（内功+外功，见 GongFaLibrary）。</summary>
        public List<string> LearnedGongFaIds;

        /// <summary>当前激活的内功 Id（null/空 = 未激活；同时仅允许激活一本）。</summary>
        public string ActiveNeiGongId;

        /// <summary>已觉醒的异能 Id（见 AwakenedPowerLibrary）。</summary>
        public List<string> AwakenedPowerIds;

        /// <summary>
        /// 特殊维度快照（吸血/反伤/回蓝/修炼速度）— 每次属性重算时由
        /// GrowthCollectProvider 的收集结果写回，供战斗事件点与 Tick 消费，
        /// 避免运行时重复遍历成长源。default 值为中性。
        /// </summary>
        public GrowthBonus Special;

        /// <summary>
        /// 结构兜底：补建 null 字段（读档后集合字段可能为 null）。
        /// 幂等，可在每次属性重算前安全调用。
        /// </summary>
        public static void Ensure(ref GrowthData growth)
        {
            if (growth == null)
            {
                growth = new GrowthData();
            }

            growth.Ensure();
        }

        /// <summary>实例级兜底（集合字段补建）。</summary>
        public void Ensure()
        {
            if (this.LingGenElements == null)
            {
                this.LingGenElements = new List<int>();
            }

            if (this.LearnedGongFaIds == null)
            {
                this.LearnedGongFaIds = new List<string>();
            }

            if (this.AwakenedPowerIds == null)
            {
                this.AwakenedPowerIds = new List<string>();
            }
        }
    }
}
