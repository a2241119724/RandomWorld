namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// 资源常量
    /// </summary>
    public class ResourceConstant
    {
        /// <summary>
        /// 预制体根目录
        /// </summary>
        public const string PREFAB_ROOT = "Prefabs/";

        /// <summary>
        /// 地图根目录
        /// </summary>
        public const string TILEMAP_ROOT = "Tilemap/";

        /// <summary>
        /// 图片根目录
        /// </summary>
        public const string IMAGE_ROOT = "Images/";

        /// <summary>
        /// 数据根目录
        /// </summary>
        public const string DATA_ROOT = "Data/";

        /// <summary>
        /// 着色器根目录
        /// </summary>
        public const string SHADER_ROOT = "Shader/";

        /// <summary>
        /// 道具数据脚本根目录
        /// </summary>
        public const string SCRIPTABLE_ROOT = "SO/";

        // =============================================================================================================

        /// <summary>
        /// 默认向量
        /// </summary>
        public static readonly Vector3 VECTOR3_DEFAULT = new (-999.0f, 0.0f, 0.0f);

        /// <summary>
        /// 默认向量整数
        /// </summary>
        public static readonly Vector3Int VECTOR3INT_DEFAULT = new (-999, 0, 0);
    }
}
