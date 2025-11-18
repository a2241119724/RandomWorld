namespace LAB2D
{
    /// <summary>
    /// 角色状态基
    /// </summary>
    public interface ICharacterState
    {
        /// <summary>
        /// 重置状态
        /// </summary>
        public void Reset();

        /// <summary>
        /// 开始执行
        /// </summary>
        public void OnEnter();

        /// <summary>
        /// 当前状态运行
        /// </summary>
        public void OnUpdate();

        /// <summary>
        /// 退出执行
        /// </summary>
        public void OnExit();
    }
}