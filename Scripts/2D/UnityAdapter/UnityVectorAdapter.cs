namespace LAB2D.UnityAdapter
{
    using LAB2D.Domain.Common;
    using UnityEngine;

    /// <summary>
    /// 在 Unity 向量类型与引擎无关的领域值对象之间进行转换。
    /// 仅在 Unity 适配器/桥接层中使用——切勿在纯领域代码中使用。
    /// </summary>
    public static class UnityVectorAdapter
    {
        /// <summary>
        /// 将 Unity Vector2 转换为领域 GameVector2。
        /// </summary>
        public static GameVector2 ToGameVector2(Vector2 value)
        {
            return new GameVector2(value.x, value.y);
        }

        /// <summary>
        /// 将领域 GameVector2 转换为 Unity Vector2。
        /// </summary>
        public static Vector2 ToUnityVector2(GameVector2 value)
        {
            return new Vector2(value.X, value.Y);
        }

        /// <summary>
        /// 将 Unity Vector3 转换为领域 GameVector2（仅使用 x、y）。
        /// </summary>
        public static GameVector2 ToGameVector2(Vector3 value)
        {
            return new GameVector2(value.x, value.y);
        }

        /// <summary>
        /// 将领域 GameVector2 转换为 Unity Vector3。
        /// </summary>
        public static Vector3 ToUnityVector3(GameVector2 value, float z = 0.0f)
        {
            return new Vector3(value.X, value.Y, z);
        }

        /// <summary>
        /// 将 Unity Vector3Int 转换为领域 GameGridPosition。
        /// </summary>
        public static GameGridPosition ToGameGridPosition(Vector3Int value)
        {
            return new GameGridPosition(value.x, value.y, value.z);
        }

        /// <summary>
        /// 将领域 GameGridPosition 转换为 Unity Vector3Int。
        /// </summary>
        public static Vector3Int ToUnityVector3Int(GameGridPosition value)
        {
            return new Vector3Int(value.X, value.Y, value.Z);
        }

        /// <summary>
        /// 将领域 GameGridPosition 转换为 Unity Vector3Int（便捷别名）。
        /// 与 ToUnityVector3Int 行为相同，命名更简洁。
        /// </summary>
        public static Vector3Int ToVector3Int(GameGridPosition value)
        {
            return new Vector3Int(value.X, value.Y, value.Z);
        }
    }
}
