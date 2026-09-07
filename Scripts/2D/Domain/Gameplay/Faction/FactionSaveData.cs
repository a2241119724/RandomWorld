namespace LAB2D.Domain.Gameplay.Faction
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// FactionManager 存档根（BinaryFormatter 可序列化：字典拍平为平行 List）。
    /// </summary>
    [Serializable]
    public class FactionManagerData
    {
        /// <summary>下一个阵营自增 ID。</summary>
        public int NextFactionId = 1;

        /// <summary>玩家阵营 ID（0=无）。</summary>
        public int PlayerFactionId;

        /// <summary>阵营列表。</summary>
        public List<FactionData> Factions = new List<FactionData>();

        /// <summary>关系矩阵 — 阵营 A 平行表（有序对小 ID）。</summary>
        public List<int> RelationA = new List<int>();

        /// <summary>关系矩阵 — 阵营 B 平行表。</summary>
        public List<int> RelationB = new List<int>();

        /// <summary>关系矩阵 — 关系值平行表。</summary>
        public List<float> RelationValue = new List<float>();

        /// <summary>交战对 — 阵营 A 平行表。</summary>
        public List<int> WarA = new List<int>();

        /// <summary>交战对 — 阵营 B 平行表。</summary>
        public List<int> WarB = new List<int>();
    }

    /// <summary>
    /// 单阵营存档数据。
    /// </summary>
    [Serializable]
    public class FactionData
    {
        /// <summary>阵营 ID。</summary>
        public int Id;

        /// <summary>阵营名。</summary>
        public string Name;

        /// <summary>色板索引。</summary>
        public int ColorIndex;

        /// <summary>现任领袖名。</summary>
        public string LeaderName;

        /// <summary>成员名单。</summary>
        public List<string> Members = new List<string>();

        /// <summary>阵营金库。</summary>
        public int TreasuryGold;

        /// <summary>贡献账本 — 名字平行表。</summary>
        public List<string> ContributionNames = new List<string>();

        /// <summary>贡献账本 — 数值平行表。</summary>
        public List<float> ContributionValues = new List<float>();

        /// <summary>核心是否已放置。</summary>
        public bool CorePlaced;

        /// <summary>核心主格 X。</summary>
        public int CoreX;

        /// <summary>核心主格 Y。</summary>
        public int CoreY;

        /// <summary>核心当前耐久。</summary>
        public float CoreHp;

        /// <summary>核心等级。</summary>
        public int CoreLevel;

        /// <summary>核心被击破次数。</summary>
        public int CoreDownfallCount;

        /// <summary>下次选举日。</summary>
        public int NextElectionDay;

        /// <summary>是否玩家创立。</summary>
        public bool IsPlayerFounded;
    }
}
