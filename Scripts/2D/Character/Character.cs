namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using Photon.Pun;
    using UnityEngine;

    /// <summary>
    /// 角色基类
    /// </summary>
    public abstract class Character : MonoBehaviourPun
    {
        /// <summary>
        /// 移动速度
        /// </summary>
        public float MoveSpeed = 2.5f;

        /// <summary>
        /// 攻击的层级
        /// </summary>
        public LayerMask AttackLayers;

        /// <summary>
        /// 攻击的标签
        /// </summary>
        public List<string> AttackTags;

        /// <summary>
        /// 闪烁
        /// </summary>
        protected SpriteRenderer spriteRenderer;

        /// <summary>
        /// 检测bug
        /// </summary>
        protected CheckBug checkBug;

        /// <summary>
        /// 角色基础属性
        /// </summary>
        protected Attribute basicAttribute;

        private Color originalColor; // 原来的自身颜色

        /// <summary>
        /// 当前装备的武器物体
        /// </summary>
        public GameObject Weapon { get; set; }

        /// <summary>
        /// 最近攻击者
        /// </summary>
        public Character LastAttacker { get; set; }

        /// <summary>
        /// 角色数据
        /// </summary>
        public CharacterData CharacterDataLAB { get; set; }

        public virtual void Awake()
        {
            this.name = this.GetType().Name;
            this.transform.SetParent(GameObject.FindGameObjectWithTag("CharacterRoot").transform);
            this.checkBug = new CheckBug();
        }

        public virtual void Start()
        {
            this.spriteRenderer = this.GetComponent<SpriteRenderer>();
            if (this.spriteRenderer == null)
            {
                LogManager.Instance.Log("renderer Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            this.originalColor = this.spriteRenderer.color;
        }

        /// <summary>
        /// 角色扣血
        /// </summary>
        /// <param name="hp">血量</param>
        /// <param name="attacker">攻击者</param>
        /// <param name="isCRT">是否暴击</param>
        public virtual void ReduceHp(float hp, Character attacker, bool isCRT = false)
        {
            if (hp <= 0)
            {
                return;
            }

            this.LastAttacker = attacker;

            // 根据防御力计算伤害
            hp -= hp * this.CharacterDataLAB.DEF / 10;
            hp = hp < 0.1f ? 0.1f : hp;

            GameObject g = ResourceManager.Instance.Instantiate(PrefabConstant.DAMAGE); // 创建物体(预设,位置,角度)
            if (g == null)
            {
                return;
            }

            // 暴击时显示不同的框
            g.GetComponent<DamageUI>().SetDamage(hp, Convert.ToInt32(isCRT));
            g.transform.SetParent(this.transform);
            g.transform.localPosition = Vector3.zero;

            // 变红
            this.spriteRenderer.color = Color.red;
            this.Invoke(nameof(this.ResetColor), 0.2f); // 一段时间后调用

            // if (!photonView.IsMine) return;
            this.CharacterDataLAB.Hp -= hp;  // 更新敌人生命值
            if (this.CharacterDataLAB.Hp <= 0)
            {
                this.CharacterDataLAB.Hp = 0;
                this.Death();
                return;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            Vector3Int posMap = TileMap.Instance.WorldPosToMapPos(this.transform.position);
            return $"{this.GetType().Name}:{this.name}\n" +
                $"速度: {this.MoveSpeed}\n" +
                $"位置: ({posMap.x},{posMap.y})\n" +
                $"物理攻击力: {this.CharacterDataLAB.ATN}\n" +
                $"魔法攻击力: {this.CharacterDataLAB.INT}\n" +
                $"物理防御力: {this.CharacterDataLAB.DEF}\n" +
                $"魔法防御力: {this.CharacterDataLAB.RES}\n" +
                $"暴击率: {this.CharacterDataLAB.CRT}\n" +
                $"暴击伤害: {this.CharacterDataLAB.CSD}\n" +
                $"速度, 回避: {this.CharacterDataLAB.SPD}\n" +
                $"命中率, 连击: {this.CharacterDataLAB.HIT}\n" +
                $"等级: {this.CharacterDataLAB.Level}\n" +
                $"经验值: {this.CharacterDataLAB.CurExperience}/{this.CharacterDataLAB.MaxExperience}\n";
        }

        /// <summary>
        /// 增加经验值.
        /// </summary>
        /// <param name="experience">经验值.</param>
        public virtual void AddExperienceValue(int experience)
        {
            this.CharacterDataLAB.CurExperience += experience;

            // 升级
            if (this.CharacterDataLAB.CurExperience / this.CharacterDataLAB.MaxExperience >= 1)
            {
                ++this.CharacterDataLAB.Level;
                this.CharacterDataLAB.CurExperience %= this.CharacterDataLAB.MaxExperience;
                this.CharacterDataLAB.MaxExperience *= 2;
                GlobalInit.Instance.ShowTip("UP " + this.CharacterDataLAB.Level);
                this.CharacterDataLAB.ComputeAttribute();
            }
        }

        /// <summary>
        /// 攻击.
        /// </summary>
        public abstract void Attack();

        /// <summary>
        /// 重置状态中的属性
        /// </summary>
        public virtual void ResetState()
        {
        }

        /// <summary>
        /// 敌人死亡
        /// </summary>
        protected virtual void Death()
        {
            GameObject.Destroy(this.gameObject);
        }

        /// <summary>
        /// 恢复颜色
        /// </summary>
        private void ResetColor()
        {
            this.spriteRenderer.color = this.originalColor;
        }

        /// <summary>
        /// 角色数据
        /// </summary>
        [Serializable]
        public class CharacterData : Attribute
        {
            /// <summary>
            /// Id
            /// </summary>
            public long Id = 0;

            /// <summary>
            /// 血量
            /// </summary>
            public float Hp = 100;

            /// <summary>
            /// 最大血量
            /// </summary>
            public float MaxHp = 100;

            /// <summary>
            /// 玩家当前经验值
            /// </summary>
            public int CurExperience = 0;

            /// <summary>
            /// 玩家当前等级最大经验值
            /// </summary>
            public int MaxExperience = 4;

            /// <summary>
            /// 当前等级
            /// </summary>
            public int Level = 1;

            /// <summary>
            /// 玩家蓝量
            /// </summary>
            public int Mp = 100;

            /// <summary>
            /// 玩家最大蓝量
            /// </summary>
            public int MaxMp = 100;

            /// <summary>
            /// 位置
            /// </summary>
            public Vector3LAB Pos;

            /// <summary>
            /// 生成的Id
            /// </summary>
            private static long generateId = 0;

            /// <summary>
            /// 生成的寻路Id
            /// </summary>
            private int generateSeekId = 0;

            /// <summary>
            /// 寻路Id
            /// </summary>
            private int seekId = 0;

            /// <summary>
            /// 携带的武器
            /// </summary>
            private AWeapon weapon;

            /// <summary>
            /// 身上携带的装备
            /// </summary>
            private Dictionary<AEquipment.EquipTypeEnum, AEquipment> equipments;

            [NonSerialized]
            private Character character;

            public CharacterData()
            {
                this.equipments = new Dictionary<AEquipment.EquipTypeEnum, AEquipment>();
                this.Id = CharacterData.generateId++;
            }

            public Character Character
            {
                get
                {
                    return this.character;
                }

                set
                {
                    this.character = value;
                    this.ComputeAttribute();
                }
            }

            public AWeapon Weapon
            {
                get
                {
                    return this.weapon;
                }

                set
                {
                    this.weapon = value;
                    this.ComputeAttribute();
                }
            }

            public string SeekId
            {
                get
                {
                    return this.Id + ":" + this.seekId;
                }
            }

            public Dictionary<AEquipment.EquipTypeEnum, AEquipment> GetEquipments()
            {
                return this.equipments;
            }

            /// <summary>
            /// 获取伤害
            /// </summary>
            /// <param name="isCRT">是否暴击</param>
            /// <returns>伤害值</returns>
            public float GetDamage(bool isCRT)
            {
                return isCRT ? this.ATN * this.CSD : this.ATN;
            }

            /// <summary>
            /// 添加装备
            /// </summary>
            /// <param name="equipment">装备</param>
            /// <param name="posMap">位置</param>
            public void AddEquipment(AEquipment equipment, Vector3Int posMap)
            {
                if (this.equipments.ContainsKey(equipment.Type))
                {
                    // 交换装备
                    AEquipment equipment1 = this.equipments[equipment.Type];
                    ItemMap.Instance.PutDownToInventory(posMap, ResourceManager.Instance.GetAsset(equipment.ToString()), new ResourceInfo(equipment.Id, 1));
                    this.equipments[equipment.Type] = equipment1;
                }
                else
                {
                    this.equipments.Add(equipment.Type, equipment);
                }

                this.ComputeAttribute();
            }

            /// <summary>
            /// 计算总属性
            /// </summary>
            public void ComputeAttribute()
            {
                float ratio = 1;
                if (this is Player.PlayerData data)
                {
                    ratio += data.Level * 0.1f;
                }

                // 基础属性
                this.ATN = this.Character.basicAttribute.ATN * ratio;
                this.INT = this.Character.basicAttribute.INT * ratio;
                this.DEF = this.Character.basicAttribute.DEF * ratio;
                this.RES = this.Character.basicAttribute.RES * ratio;
                this.CRT = this.Character.basicAttribute.CRT * ratio;
                this.CSD = this.Character.basicAttribute.CSD * ratio;
                this.SPD = this.Character.basicAttribute.SPD * ratio;
                this.HIT = this.Character.basicAttribute.HIT * ratio;

                if (this.weapon != null)
                {
                    this.ATN += this.weapon.Attribute.ATN;
                    this.INT += this.weapon.Attribute.INT;
                    this.DEF += this.weapon.Attribute.DEF;
                    this.RES += this.weapon.Attribute.RES;
                    this.CRT += this.weapon.Attribute.CRT;
                    this.CSD += this.weapon.Attribute.CSD;
                    this.SPD += this.weapon.Attribute.SPD;
                    this.HIT += this.weapon.Attribute.HIT;
                }

                foreach (var item in this.equipments)
                {
                    this.ATN += item.Value.Attribute.ATN;
                    this.INT += item.Value.Attribute.INT;
                    this.DEF += item.Value.Attribute.DEF;
                    this.RES += item.Value.Attribute.RES;
                    this.CRT += item.Value.Attribute.CRT;
                    this.CSD += item.Value.Attribute.CSD;
                    this.SPD += item.Value.Attribute.SPD;
                    this.HIT += item.Value.Attribute.HIT;
                }
            }

            /// <summary>
            /// 生成寻路Id
            /// </summary>
            /// <returns>寻路Id</returns>
            public string GenerateSeekId()
            {
                this.seekId = ++this.generateSeekId;
                return this.SeekId;
            }
        }

        public class Attribute
        {
            /// <summary>
            /// 物理攻击力
            /// </summary>
            public float ATN;

            /// <summary>
            /// 魔法攻击力
            /// </summary>
            public float INT;

            /// <summary>
            /// 物理防御力
            /// </summary>
            public float DEF;

            /// <summary>
            /// 魔法防御力
            /// </summary>
            public float RES;

            /// <summary>
            /// 暴击率
            /// </summary>
            public float CRT;

            /// <summary>
            /// 暴击伤害
            /// </summary>
            public float CSD;

            /// <summary>
            /// 速度，回避物理攻击之类的
            /// </summary>
            public float SPD;

            /// <summary>
            /// 命中率或者连击之类的
            /// </summary>
            public float HIT;

            public Attribute()
            {
                this.ATN = 0;
                this.INT = 0;
                this.DEF = 0;
                this.RES = 0;
                this.CRT = 0;
                this.CSD = 0;
                this.SPD = 0;
                this.HIT = 0;
            }

            public Attribute(float atn, float int_, float def, float res, float crt, float csd, float spd, float hit)
            {
                this.ATN = atn;
                this.INT = int_;
                this.DEF = def;
                this.RES = res;
                this.CRT = crt;
                this.CSD = csd;
                this.SPD = spd;
                this.HIT = hit;
            }
        }

        /// <summary>
        /// 检查bug
        /// </summary>
        protected class CheckBug
        {
            /// <summary>
            /// 上一次碰撞时间
            /// </summary>
            public long LastTime;

            /// <summary>
            /// 连续碰撞次数
            /// </summary>
            public int ColliderCount;

            private const double Interval = 3e5;
            private const int Threshold = 100;

            /// <summary>
            /// 是否有bug
            /// </summary>
            /// <param name="name">出bug的角色名称</param>
            /// <param name="threshold">检测bug的碰撞阈值</param>
            /// <returns>是否有bug.</returns>
            public bool IsBug(string name, int threshold = Threshold)
            {
                bool bug = this.ColliderCount > threshold;
                if (bug)
                {
                    // LogManager.Instance.log(name + "碰撞次数:" + colliderCount, LogManager.LogLevel.Info);
                }

                return bug;
            }

            /// <summary>
            /// 添加碰撞次数
            /// </summary>
            /// <param name="time">当前时间</param>
            public void AddColliderCount(long time)
            {
                if (time - this.LastTime < Interval)
                {
                    this.ColliderCount++;
                }
                else
                {
                    this.ColliderCount = 1;
                }

                this.LastTime = time;
            }
        }
    }
}
