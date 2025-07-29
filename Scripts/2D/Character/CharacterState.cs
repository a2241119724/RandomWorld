namespace LAB2D
{
    /// <summary>
    /// 角色状态
    /// </summary>
    /// <typeparam name="C">Character</typeparam>
    public abstract class CharacterState<C> : ICharacterState
        where C : Character
    {
        protected CharacterState(C character)
        {
            this.Character = character;
        }

        /// <summary>
        /// 角色
        /// </summary>
        public C Character { get; set; }

        /// <inheritdoc/>
        public virtual void OnEnter()
        {
        }

        /// <inheritdoc/>
        public virtual void OnExit()
        {
        }

        /// <inheritdoc/>
        public virtual void OnUpdate()
        {
        }
    }

    /// <summary>
    /// 角色状态基
    /// </summary>
    public interface ICharacterState
    {
        /// <summary>
        /// 角色
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