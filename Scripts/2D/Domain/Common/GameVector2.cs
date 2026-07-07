namespace LAB2D
{
    /// <summary>
    /// Engine-agnostic 2D vector used by pure gameplay rules.
    /// </summary>
    public readonly struct GameVector2
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
    }
}
