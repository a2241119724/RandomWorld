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

        // tag =============================================================================================================

        /// <summary>
        /// UI标签
        /// </summary>
        public const string UI_TAG = "UIRoot";

        /// <summary>
        /// 角色标签
        /// </summary>
        public const string CHARACTER_TAG = "CharacterRoot";

        /// <summary>
        /// 地图标签
        /// </summary>
        public const string TILEMAP_TAG = "TileMap";

        /// <summary>
        /// 资源地图标签
        /// </summary>
        public const string RESOURCE_MAP_TAG = "ResourceMap";

        /// <summary>
        /// 全局光照标签
        /// </summary>
        public const string GLOBAL_LIGHT_TAG = "GlobalLight";

        /// <summary>
        /// 小地图标签
        /// </summary>
        public const string MINIMAP_TAG = "MiniMap";

        /// <summary>
        /// 动作UI标签
        /// </summary>
        public const string ACTION_UI_TAG = "ActionUI";

        // =============================================================================================================

        /// <summary>
        /// 默认向量
        /// </summary>
        public static readonly Vector3 VECTOR3_DEFAULT = new Vector3(-999.0f, 0.0f, 0.0f);

        /// <summary>
        /// 默认向量整数
        /// </summary>
        public static readonly Vector3Int VECTOR3INT_DEFAULT = new Vector3Int(-999, 0, 0);
    }
}
