namespace LAB2D.Domain.Gameplay.Faction
{
    using System.Collections.Generic;

    /// <summary>
    /// 阵营核心运行时状态（纯数据）。位置存 int 格坐标（Gameplay 层转 Vector3Int）。
    /// </summary>
    public class FactionCoreState
    {
        /// <summary>核心是否已放置（含读档恢复）。</summary>
        public bool IsPlaced;

        /// <summary>主格（BottomLeft 参考点）X 格坐标。</summary>
        public int X;

        /// <summary>主格（BottomLeft 参考点）Y 格坐标。</summary>
        public int Y;

        /// <summary>当前耐久。</summary>
        public float Hp;

        /// <summary>核心等级（1 起）。</summary>
        public int Level = 1;

        /// <summary>已被击破次数（超 MaxDownfalls → 阵营解散）。</summary>
        public int DownfallCount;
    }

    /// <summary>
    /// Worker 阵营实体（Domain 纯数据，零 Unity 依赖）。
    /// 职位不落存档——由 FactionRankRuleService 按贡献阈值派生（P2）。
    /// 成员名即 ID（与 ItemOwnershipService 同一假设：Worker 稳定名不重复）。
    /// </summary>
    public class WorkerFaction
    {
        /// <summary>阵营 ID（自增 1 起，0=无阵营，与 WorkerData.FactionId 对应）。</summary>
        public int FactionId;

        /// <summary>阵营名（修仙风名称池分配）。</summary>
        public string Name;

        /// <summary>色板索引（8 色，徽标/HUD 染色用）。</summary>
        public int ColorIndex;

        /// <summary>现任领袖名（创始人即首任领袖；P3 起由选举产生）。</summary>
        public string LeaderName;

        /// <summary>成员 Worker 名单（领袖也在内）。</summary>
        public List<string> MemberNames = new List<string>();

        /// <summary>阵营金库（创始资金入此，P2 起核心升级从这里扣）。</summary>
        public int TreasuryGold;

        /// <summary>贡献点独立账本（成员名 → 累计贡献，P2 起有入账）。</summary>
        public Dictionary<string, float> Contribution = new Dictionary<string, float>();

        /// <summary>阵营核心状态。</summary>
        public FactionCoreState Core = new FactionCoreState();

        /// <summary>下次选举日（游戏日索引，P3 选举调度用，先落存档占位）。</summary>
        public int NextElectionDay;

        /// <summary>是否玩家创立的阵营（玩家阵营不占 Worker 成员位）。</summary>
        public bool IsPlayerFounded;
    }
}
