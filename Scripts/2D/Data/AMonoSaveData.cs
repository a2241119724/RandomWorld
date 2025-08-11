namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// 带有MonoBehaviour的ISaveData
    /// </summary>
    public abstract class AMonoSaveData : MonoBehaviour, ISaveData
    {
        /// <summary>
        /// 加载数据.
        /// </summary>
        public virtual void LoadData()
        {
        }

        /// <summary>
        /// 保存数据.
        /// </summary>
        public virtual void SaveData()
        {
        }
    }
}