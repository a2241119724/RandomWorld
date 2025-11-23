namespace LAB2D
{
    /// <summary>
    /// 角色状态
    /// </summary>
    /// <typeparam name="C">Character</typeparam>
    public abstract class ACharacterState<C> : ICharacterState
        where C : Character
    {
        protected ACharacterState(C character)
        {
            this.Character = character;
        }

        /// <summary>
        /// 角色
        /// </summary>
        public C Character { get; set; }

        /// <inheritdoc/>
        public virtual void Reset()
        {
        }

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
}