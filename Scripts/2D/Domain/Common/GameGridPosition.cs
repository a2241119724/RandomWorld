namespace LAB2D.Domain.Common
{
    using System;

    /// <summary>
    /// 与引擎无关的网格位置值对象。
    /// 在纯领域代码中替代UnityEngine.Vector3Int。
    /// </summary>
    public readonly struct GameGridPosition : IEquatable<GameGridPosition>
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public GameGridPosition(int x, int y, int z = 0)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public static GameGridPosition operator +(GameGridPosition a, GameGridPosition b)
        {
            return new GameGridPosition(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static GameGridPosition operator -(GameGridPosition a, GameGridPosition b)
        {
            return new GameGridPosition(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static bool operator ==(GameGridPosition a, GameGridPosition b)
        {
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        }

        public static bool operator !=(GameGridPosition a, GameGridPosition b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            return obj is GameGridPosition other && this == other;
        }

        public bool Equals(GameGridPosition other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = (hash * 31) + this.X;
            hash = (hash * 31) + this.Y;
            hash = (hash * 31) + this.Z;
            return hash;
        }

        public override string ToString()
        {
            return $"({this.X}, {this.Y}, {this.Z})";
        }
    }
}
