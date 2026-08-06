namespace LAB2D.Domain.Common
{
    using System;

    /// <summary>
    /// 与引擎无关的二维向量，供纯玩法规则使用。
    /// </summary>
    [Serializable]
    public readonly struct GameVector2 : IEquatable<GameVector2>
    {
        public readonly float X;
        public readonly float Y;

        public GameVector2(float x, float y)
        {
            this.X = x;
            this.Y = y;
        }

        public float SqrDistanceTo(GameVector2 other)
        {
            float dx = this.X - other.X;
            float dy = this.Y - other.Y;
            return (dx * dx) + (dy * dy);
        }

        public static GameVector2 operator +(GameVector2 a, GameVector2 b)
        {
            return new GameVector2(a.X + b.X, a.Y + b.Y);
        }

        public static GameVector2 operator -(GameVector2 a, GameVector2 b)
        {
            return new GameVector2(a.X - b.X, a.Y - b.Y);
        }

        public static GameVector2 operator *(GameVector2 v, float scalar)
        {
            return new GameVector2(v.X * scalar, v.Y * scalar);
        }

        public static GameVector2 operator *(float scalar, GameVector2 v)
        {
            return new GameVector2(v.X * scalar, v.Y * scalar);
        }

        public static bool operator ==(GameVector2 a, GameVector2 b)
        {
            return a.X == b.X && a.Y == b.Y;
        }

        public static bool operator !=(GameVector2 a, GameVector2 b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            return obj is GameVector2 other && this == other;
        }

        public bool Equals(GameVector2 other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = (hash * 31) + this.X.GetHashCode();
            hash = (hash * 31) + this.Y.GetHashCode();
            return hash;
        }

        public override string ToString()
        {
            return $"({this.X:F2}, {this.Y:F2})";
        }
    }
}
