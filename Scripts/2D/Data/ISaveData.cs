namespace LAB2D.Data
{
    using LAB2D;
    /// <summary>
    /// 赋予保存与读取数据的功能.
    /// </summary>
    public interface ISaveData
    {
        /// <summary>
        /// 加载数据.
        /// </summary>
        void LoadData();

        /// <summary>
        /// 保存数据.
        /// </summary>
        void SaveData();
    }
}
