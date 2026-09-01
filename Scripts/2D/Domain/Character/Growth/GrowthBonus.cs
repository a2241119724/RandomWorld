namespace LAB2D.Domain.Character.Growth
{
    /// <summary>
    /// 成长源加成 — 单一成长系统（词条/内功/灵根/境界等）对角色的贡献。
    /// <see cref="Stats"/> 进入 AttributeCalculationService 常规加法管线；
    /// MaxHpFlat/MaxMpFlat 作用于生命/法力上限（8 维之外）；
    /// 其余为特殊效果维度，不进 BattleStats，由各系统在事件点（攻击/受击/Tick）消费。
    /// 倍率类维度（CultivationSpeedMul）以"加数"形式存储：+0.2 表示 +20%，消费方 +1 使用，
    /// 因此 default/GrowthBonus.Zero 对所有维度均为中性值。
    /// </summary>
    public readonly struct GrowthBonus
    {
        /// <summary>8 维战斗属性加成（加法）。</summary>
        public readonly BattleStats Stats;

        /// <summary>生命上限加成（平坦值）。</summary>
        public readonly float MaxHpFlat;

        /// <summary>法力上限加成（平坦值）。</summary>
        public readonly float MaxMpFlat;

        /// <summary>每秒回蓝。</summary>
        public readonly float MpRegenPerSec;

        /// <summary>吸血比例（0.05 = 造成伤害的 5% 转为回血）。</summary>
        public readonly float LifestealRatio;

        /// <summary>反伤比例（0.1 = 受到伤害的 10% 反弹给攻击者）。</summary>
        public readonly float ReflectRatio;

        /// <summary>修炼速度加数（0.2 = 打坐灵气获取 +20%），消费方按 1 + 值 使用。</summary>
        public readonly float CultivationSpeedMul;

        public GrowthBonus(
            BattleStats stats,
            float maxHpFlat = 0f,
            float maxMpFlat = 0f,
            float mpRegenPerSec = 0f,
            float lifestealRatio = 0f,
            float reflectRatio = 0f,
            float cultivationSpeedMul = 0f)
        {
            this.Stats = stats;
            this.MaxHpFlat = maxHpFlat;
            this.MaxMpFlat = maxMpFlat;
            this.MpRegenPerSec = mpRegenPerSec;
            this.LifestealRatio = lifestealRatio;
            this.ReflectRatio = reflectRatio;
            this.CultivationSpeedMul = cultivationSpeedMul;
        }

        /// <summary>中性值：所有维度为零。</summary>
        public static GrowthBonus Zero
        {
            get { return default; }
        }

        public static GrowthBonus operator +(GrowthBonus a, GrowthBonus b)
        {
            return new GrowthBonus(
                a.Stats + b.Stats,
                a.MaxHpFlat + b.MaxHpFlat,
                a.MaxMpFlat + b.MaxMpFlat,
                a.MpRegenPerSec + b.MpRegenPerSec,
                a.LifestealRatio + b.LifestealRatio,
                a.ReflectRatio + b.ReflectRatio,
                a.CultivationSpeedMul + b.CultivationSpeedMul);
        }
    }
}
