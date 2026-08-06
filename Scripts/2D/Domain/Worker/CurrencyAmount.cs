namespace LAB2D.Domain.Worker
{
    using System;

    /// <summary>
    /// 货币值对象 — 纯 C# 不可变值类型，供 Worker 经济系统使用。
    /// 遵循 GameVector2 / GameGridPosition 的模式，不依赖 UnityEngine。
    /// </summary>
    [Serializable]
    public readonly struct CurrencyAmount : IEquatable<CurrencyAmount>
    {
        public readonly int Gold;

        public CurrencyAmount(int gold)
        {
            this.Gold = gold;
        }

        public static CurrencyAmount Zero => new CurrencyAmount(0);

        public static CurrencyAmount operator +(CurrencyAmount a, CurrencyAmount b)
        {
            return new CurrencyAmount(a.Gold + b.Gold);
        }

        public static CurrencyAmount operator -(CurrencyAmount a, CurrencyAmount b)
        {
            return new CurrencyAmount(a.Gold - b.Gold);
        }

        /// <summary>
        /// 余额是否足够支付指定金额。
        /// </summary>
        public bool HasEnough(CurrencyAmount cost)
        {
            return this.Gold >= cost.Gold;
        }

        public static bool operator ==(CurrencyAmount a, CurrencyAmount b)
        {
            return a.Gold == b.Gold;
        }

        public static bool operator !=(CurrencyAmount a, CurrencyAmount b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            return obj is CurrencyAmount other && this == other;
        }

        public bool Equals(CurrencyAmount other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return this.Gold.GetHashCode();
        }

        public override string ToString()
        {
            return $"{this.Gold}G";
        }
    }
}
