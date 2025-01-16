using System.Collections.Generic;
using UnityEngine;

namespace LAB2D {
    public abstract class CharacterManager<CM,C,CC> : ASingletonSaveData<CM>,ICharacterManager<C>,ICharacterCreator where CM : new() where C : Character where CC : ICharacterCreator,new()
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

        public virtual GameObject create(Vector3 worldPos=default)
        {
            GameObject g = creator.create(worldPos);
            if (g == null) return null;
            Characters.Add(g.GetComponent<C>());
            return g;
        }

        public override void loadData()
        {
            base.loadData();
            List<Character.CharacterData> data = Tool.loadDataByBinary<List<Character.CharacterData>>(GlobalData.ConfigFile.getPath(this.GetType().Name));
            foreach(Character.CharacterData characterData in data)
            {
                GameObject g = create(Vector3LAB.toVector3(characterData.pos));
                g.GetComponent<C>().CharacterDataLAB = characterData;
            }
        }

        public override void saveData()
        {
            base.saveData();
            List<Character.CharacterData> characterDatas = new List<Character.CharacterData>();
            foreach (C character in Characters)
            {
                character.CharacterDataLAB.pos = Vector3LAB.toVector3LAB(character.transform.position);
                characterDatas.Add(character.CharacterDataLAB);
            }
            Tool.saveDataByBinary(GlobalData.ConfigFile.getPath(this.GetType().Name), characterDatas);
        }

        public C getCharacterByPos(Vector3Int posMap) { 
            foreach(C character in Characters)
            {
                Vector3Int characterPosMap = TileMap.Instance.worldPosToMapPos(character.transform.position);
                if(characterPosMap.x == posMap.x && characterPosMap.y == posMap.y)
                {
                    return character;
                }
            }
            return null;
        }
    }

    public interface ICharacterManager<C>
    {
        /// <summary>
        /// 添加角色
        /// </summary>
        void add(C character);
        /// <summary>
        /// 删除角色
        /// </summary>
        void remove(C character);
        /// <summary>
        /// 获取角色
        /// </summary>
        /// <param name="i">索引</param>
        /// <returns>角色</returns>
        C get(int i);
        /// <summary>
        /// 获取角色数量
        /// </summary>
        /// <returns>数量</returns>
        int count();
    }
}
