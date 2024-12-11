using System.Collections.Generic;
using UnityEngine;

namespace LAB2D {
    public abstract class CharacterManager<CM,C,CC> : Singleton<CM>,ICharacterManager<C>,ICharacterCreator where CM : new() where C : Character where CC : ICharacterCreator,new()
    {
        public List<C> Characters { get; set; }
        private CC creator;

        public CharacterManager() {
            Characters = new List<C>();
            creator = CharacterCreator<CC>.Instance;
        }

        public virtual void add(C character)
        {
            if (character == null)
            {
                Debug.LogError("character is null!!!");
                return;
            }
            Characters.Add(character);
        }

        public virtual void remove(C character)
        {
            if (character == null)
            {
                Debug.LogError("character is null!!!");
                return;
            }
            Characters.Remove(character);
        }

        public virtual C get(int i)
        {
            if (i < 0 || i >= count())
            {
                Debug.LogError("i overflow!!!");
                return null;
            }
            return Characters[i];
        }

        public int count()
        {
            return Characters.Count;
        }

        public virtual GameObject create()
        {
            GameObject g = creator.create();
            if (g == null) return null;
            Characters.Add(g.GetComponent<C>());
            return g;
        }
    }

    public interface ICharacterManager<C>
    {
        /// <summary>
        /// ��ӽ�ɫ
        /// </summary>
        void add(C character);
        /// <summary>
        /// ɾ����ɫ
        /// </summary>
        void remove(C character);
        /// <summary>
        /// ��ȡ��ɫ
        /// </summary>
        /// <param name="i">����</param>
        /// <returns>��ɫ</returns>
        C get(int i);
        /// <summary>
        /// ��ȡ��ɫ����
        /// </summary>
        /// <returns>����</returns>
        int count();
    }
}
