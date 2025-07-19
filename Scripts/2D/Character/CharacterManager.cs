namespace LAB2D
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 角色管理器
    /// </summary>
    /// <typeparam name="CM">角色管理</typeparam>
    /// <typeparam name="C">角色</typeparam>
    /// <typeparam name="CC">角色控制</typeparam>
    public abstract class CharacterManager<CM, C, CC> : ASingletonSaveData<CM>, ICharacterManager<C>, ICharacterCreator
        where CM : new()
        where C : Character
        where CC : ICharacterCreator, new()
    {
        private CC creator; // 角色创建器

        public CharacterManager()
        {
            this.Characters = new List<C>();
            this.creator = CharacterCreator<CC>.Instance;
        }

        /// <summary>
        /// 所有角色
        /// </summary>
        public List<C> Characters { get; set; }

        /// <summary>
        /// 增加角色
        /// </summary>
        /// <param name="character">角色</param>
        public virtual void Add(C character)
        {
            if (character == null)
            {
                LogManager.Instance.Log("character is null!!!", LogManager.LogLevel.Error);
                return;
            }

            this.Characters.Add(character);
        }

        /// <summary>
        /// 移除角色
        /// </summary>
        /// <param name="character">角色</param>
        public virtual void Remove(C character)
        {
            if (character == null)
            {
                LogManager.Instance.Log("character is null!!!", LogManager.LogLevel.Error);
                return;
            }

            this.Characters.Remove(character);
        }

        /// <summary>
        /// 获取角色
        /// </summary>
        /// <param name="i">索引</param>
        /// <returns>角色</returns>
        public virtual C Get(int i)
        {
            if (i < 0 || i >= this.Count())
            {
                LogManager.Instance.Log("i overflow!!!", LogManager.LogLevel.Error);
                return null;
            }

            return this.Characters[i];
        }

        /// <summary>
        /// 获取角色数量
        /// </summary>
        /// <returns>数量</returns>
        public int Count()
        {
            return this.Characters.Count;
        }

        /// <summary>
        /// 创建角色
        /// </summary>
        /// <param name="worldPos">角色位置</param>
        /// <returns>角色</returns>
        public virtual GameObject create(Vector3 worldPos = default)
        {
            GameObject g = this.creator.create(worldPos);
            if (g == null)
            {
                return null;
            }

            this.Add(g.GetComponent<C>());
            return g;
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            base.LoadData();
            List<Character.CharacterData> data = Tool.LoadDataByBinary<List<Character.CharacterData>>(GlobalData.ConfigFile.getPath(this.GetType().Name));
            foreach (Character.CharacterData characterData in data)
            {
                GameObject g = this.create(Vector3LAB.ToVector3(characterData.Pos));
                g.GetComponent<C>().CharacterDataLAB = characterData;
            }
        }

        /// <inheritdoc/>
        public override void SaveData()
        {
            base.SaveData();
            List<Character.CharacterData> characterDatas = new List<Character.CharacterData>();
            foreach (C character in this.Characters)
            {
                character.CharacterDataLAB.Pos = Vector3LAB.ToVector3LAB(character.transform.position);
                characterDatas.Add(character.CharacterDataLAB);
            }

            Tool.SaveDataByBinary(GlobalData.ConfigFile.getPath(this.GetType().Name), characterDatas);
        }

        /// <summary>
        /// 获取指定地图坐标的玩家
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <returns>角色</returns>
        public C GetCharacterByPos(Vector3Int posMap)
        {
            foreach (C character in this.Characters)
            {
                Vector3 worldPos = TileMap.Instance.mapPosToWorldPos(posMap);
                if (Mathf.Sqrt(Mathf.Pow(character.transform.position.x - worldPos.x, 2)
                    + Mathf.Pow(character.transform.position.y - worldPos.y, 2)) < 0.7f)
                {
                    return character;
                }
            }

            return null;
        }
    }

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
