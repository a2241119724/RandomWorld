using UnityEngine;

namespace LAB2D
{
    public abstract class CharacterState<C> : ICharacterState where C : Character
    {
        public C Character { set; get; }
        
        protected CharacterState(C character)
		{
			this.Character = character;
		}

        public virtual void OnEnter()
        {
        }

        public virtual void OnExit()
        {
        }

        public virtual void OnUpdate()
        {
        }
    }

    public interface ICharacterState
    {
        public void OnEnter();
        public void OnUpdate();
        public void OnExit();
    }
}