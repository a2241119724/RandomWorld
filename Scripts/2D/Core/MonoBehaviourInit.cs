namespace LAB2D.Core
{
    using UnityEngine;

    /// <summary>
    /// 带 Init 方法的 MonoBehaviour 基类。
    /// 提供统一的初始化入口，子类在 Awake/Start 之外额外暴露 Init() 供外部显式调用。
    /// </summary>
    public abstract class MonoBehaviourInit : MonoBehaviour
    {
        /// <summary>
        /// 初始化方法。子类可重写以执行自定义初始化逻辑。
        /// </summary>
        public virtual void Init()
        {
        }
    }
}
