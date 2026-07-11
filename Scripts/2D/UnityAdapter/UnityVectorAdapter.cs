namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// Converts between Unity vector types and engine-agnostic domain value objects.
    /// Use in Unity adapter/bridge layer only — never in pure domain code.
    /// </summary>
    public static class UnityVectorAdapter
    {
        /// <summary>
        /// Convert Unity Vector2 to domain GameVector2.
        /// </summary>
        public static GameVector2 ToGameVector2(Vector2 value)
        {
            return new GameVector2(value.x, value.y);
        }

        /// <summary>
        /// Convert domain GameVector2 to Unity Vector2.
        /// </summary>
        public static Vector2 ToUnityVector2(GameVector2 value)
        {
            return new Vector2(value.X, value.Y);
        }

        /// <summary>
        /// Convert Unity Vector3 to domain GameVector2 (uses only x, y).
        /// </summary>
        public static GameVector2 ToGameVector2(Vector3 value)
        {
            return new GameVector2(value.x, value.y);
        }

        /// <summary>
        /// Convert domain GameVector2 to Unity Vector3.
        /// </summary>
        public static Vector3 ToUnityVector3(GameVector2 value, float z = 0.0f)
        {
            return new Vector3(value.X, value.Y, z);
        }

        /// <summary>
        /// Convert Unity Vector3Int to domain GameGridPosition.
        /// </summary>
        public static GameGridPosition ToGameGridPosition(Vector3Int value)
        {
            return new GameGridPosition(value.x, value.y, value.z);
        }

        /// <summary>
        /// Convert domain GameGridPosition to Unity Vector3Int.
        /// </summary>
        public static Vector3Int ToUnityVector3Int(GameGridPosition value)
        {
            return new Vector3Int(value.X, value.Y, value.Z);
        }
    }
}
