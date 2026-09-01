namespace LAB2D.Character
{
    using LAB2D;
    using LAB2D.Serializable;
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
        /// <summary>
        /// 角色创建器
        /// </summary>
        protected readonly CC creator;

        public CharacterManager()
        {
            this.Characters = new List<C>();
            this.creator = Core.ServiceLocator.Get<CC>();
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
                AWorkerTask.LogProvider("character is null!!!", LogManager.LogLevelEnum.Error);
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
                AWorkerTask.LogProvider("character is null!!!", LogManager.LogLevelEnum.Error);
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
                AWorkerTask.LogProvider("i overflow!!!", LogManager.LogLevelEnum.Error);
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
        public virtual GameObject Create(Vector3 worldPos = default)
        {
            GameObject g = this.creator.Create(worldPos);
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
            Core.GameServices.AsyncProgressSetTipProvider("加载角色管理信息...");
            List<Character.CharacterData> data = DataTool.LoadDataByBinary<List<Character.CharacterData>>(GlobalData.ConfigFile.GetPath(this.GetType().Name));
            if (data == null)
            {
                return;
            }

            foreach (Character.CharacterData characterData in data)
            {
                GameObject g = this.Create(Vector3LAB.ToVector3(characterData.Pos));
                C character = g.GetComponent<C>();
                character.CharacterDataLAB = characterData;

                // 恢复名字（覆盖 Create 过程中随机生成的名字）
                if (!string.IsNullOrEmpty(characterData.Name))
                {
                    character.name = characterData.Name;
                }

                // 心智层读档兜底：BinaryFormatter 反序列化不跑构造函数，
                // 老档 Mind 可能为 null，此处统一补建（AWorker.Start 与服务方法也有 Ensure 兜底）。
                if (character is AWorker worker)
                {
                    // CharacterData.Character 反向引用是 [NonSerialized]，读档必须重连（仿 PlayerManager），
                    // 否则 Worker 换装/成长系统的 RecomputeGrowthAttributes 会被静默跳过。
                    worker.CharacterDataLAB.Character = worker;
                    WorkerMindData.Ensure(worker.CharacterDataLAB as AWorker.WorkerData);
                }
            }
        }

        /// <inheritdoc/>
        public override void SaveData()
        {
            base.SaveData();
            List<Character.CharacterData> characterDatas = new ();
            foreach (C character in this.Characters)
            {
                character.CharacterDataLAB.Pos = Vector3LAB.ToVector3LAB(character.transform.position);
                character.CharacterDataLAB.Name = character.name;
                characterDatas.Add(character.CharacterDataLAB);
            }

            DataTool.SaveDataByBinary(GlobalData.ConfigFile.GetPath(this.GetType().Name), characterDatas);
        }

        /// <summary>
        /// 获取指定地图坐标的玩家
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <returns>角色</returns>
        public C GetCharacterByPos(Vector3Int posMap)
        {
            Vector3 worldPos = AWorkerTask.TileMapPositionProvider(posMap);
            foreach (C character in this.Characters)
            {
                Vector3 cPos = character.transform.position;
                float dx = cPos.x - worldPos.x;
                float dy = cPos.y - worldPos.y;
                if ((dx * dx) + (dy * dy) < 0.49f)
                {
                    return character;
                }
            }

            return null;
        }
    }
}
