namespace LAB2D
{
    /// <summary>
    /// Engine-agnostic grid position value object.
    /// Replaces UnityEngine.Vector3Int in pure domain code.
    /// </summary>
    public readonly struct GameGridPosition
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

        public override string ToString()
        {
            return $"({this.X}, {this.Y}, {this.Z})";
        }

        public override bool Equals(object obj)
        {
            return obj is GameGridPosition other &&
                this.X == other.X &&
                this.Y == other.Y &&
                this.Z == other.Z;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = (hash * 31) + this.X;
            hash = (hash * 31) + this.Y;
            hash = (hash * 31) + this.Z;
            return hash;
        }
    }
}
