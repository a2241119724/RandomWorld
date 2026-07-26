namespace LAB2D.Character
{
    using LAB2D;
    /// <summary>
    /// 角色管理接口
    /// </summary>
    /// <typeparam name="C">角色</typeparam>
    public interface ICharacterManager<C>
    {
        /// <summary>
        /// 添加角色
        /// </summary>
        /// <param name="character">角色</param>
        void Add(C character);

        /// <summary>
        /// 删除角色
        /// </summary>
        /// <param name="character">角色</param>
        void Remove(C character);

        /// <summary>
        /// 获取角色
        /// </summary>
        /// <param name="i">索引</param>
        /// <returns>角色</returns>
        C Get(int i);

        /// <summary>
        /// 获取角色数量
        /// </summary>
        /// <returns>数量</returns>
        int Count();
    }
}