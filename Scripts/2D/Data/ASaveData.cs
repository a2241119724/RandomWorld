namespace LAB2D.Data
{
    using LAB2D;
    /// <summary>
    /// 抽象保存数据
    /// </summary>
    public abstract class ASaveData : ISaveData
    {
        /// <summary>
        /// 加载数据
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
