namespace LAB2D
{
    /// <summary>
    /// 角色状态管理
    /// </summary>
    /// <typeparam name="CS">CharacterState</typeparam>
    /// <typeparam name="CST">CharacterStateType</typeparam>
    public interface ICharacterStateManager<CS, CST>
    {
        /// <summary>
        /// 添加状态
        /// </summary>
        /// <param name="type">状态类型</param>
        /// <param name="state">状态</param>
        void AddState(CST type, CS state);

        /// <summary>
        /// 改变状态
        /// </summary>
        /// <param name="type">状态类型</param>
        void ChangeState(CST type);
    }
}