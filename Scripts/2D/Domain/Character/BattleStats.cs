namespace LAB2D.Domain.Character
{
    /// <summary>
    /// 战斗属性 — 纯 C# 值对象，8 个战斗维度。
    /// 不依赖 UnityEngine，可独立用于属性计算和单元测试。
    /// </summary>
    public readonly struct BattleStats
    {
        public readonly float ATN;
        public readonly float INT;
        public readonly float DEF;
        public readonly float RES;
        public readonly float CRT;
        public readonly float CSD;
        public readonly float SPD;
        public readonly float HIT;

        public BattleStats(float atn, float int_, float def, float res, float crt, float csd, float spd, float hit)
        {
            this.ATN = atn;
            this.INT = int_;
            this.DEF = def;
            this.RES = res;
            this.CRT = crt;
            this.CSD = csd;
            this.SPD = spd;
            this.HIT = hit;
        }

        public static BattleStats Zero
        {
            get { return new BattleStats(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f); }
        }

        public static BattleStats operator +(BattleStats a, BattleStats b)
        {
            return new BattleStats(
                a.ATN + b.ATN,
                a.INT + b.INT,
                a.DEF + b.DEF,
                a.RES + b.RES,
                a.CRT + b.CRT,
                a.CSD + b.CSD,
                a.SPD + b.SPD,
                a.HIT + b.HIT);
        }

        public static BattleStats operator *(BattleStats a, float scalar)
        {
            return new BattleStats(
                a.ATN * scalar,
                a.INT * scalar,
                a.DEF * scalar,
                a.RES * scalar,
                a.CRT * scalar,
                a.CSD * scalar,
                a.SPD * scalar,
                a.HIT * scalar);
        }
    }
}
