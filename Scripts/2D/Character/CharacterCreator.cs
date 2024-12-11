using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D {
    public abstract class CharacterCreator<T> : Singleton<T>,ICharacterCreator where T : new()
    {
        protected abstract GameObject _create();
        public virtual GameObject create()
        {
            return _create();
        }
    }

    public interface ICharacterCreator
    {
        GameObject create();
    }
}
