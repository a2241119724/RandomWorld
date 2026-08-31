namespace LAB2D.Constant
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
        /// 动画根目录
        /// </summary>
        public const string ANIMATION_ROOT = "Animation/";

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

        /// <summary>
        /// StreamingAssets 下的内置 AI 目录
        /// </summary>
        public const string STREAMING_AI_ROOT = "AI";

        /// <summary>
        /// StreamingAssets 下的内置模型目录
        /// </summary>
        public const string STREAMING_MODEL_ROOT = STREAMING_AI_ROOT + "/models";

        /// <summary>
        /// 内置 LLM 模型文件名
        /// </summary>
        public const string BUILTIN_LLM_MODEL_FILE = "Qwen2.5-0.5B-Instruct-Q8_0.gguf";

        /// <summary>
        /// 内置 LLM 模型在 StreamingAssets 下的相对路径
        /// </summary>
        public const string BUILTIN_LLM_MODEL_RELATIVE_PATH = STREAMING_MODEL_ROOT + "/" + BUILTIN_LLM_MODEL_FILE;

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
