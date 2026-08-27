namespace LAB2D.Domain.Worker
{
    using LAB2D.Character.Worker;
    using System;
    using System.Collections.Generic;

    /// <summary>事件记忆的正负向。</summary>
    [Serializable]
    public enum MemoryValence { Neutral, Positive, Negative }

    /// <summary>关系类型。</summary>
    [Serializable]
    public enum RelationKind { None, Friendship, Enmity, Admiration, Grudge }

    /// <summary>个人执念类型 — 超越现有 4 类固定目标（WorkerGoal）的个人梦想。</summary>
    [Serializable]
    public enum WorkerDreamType
    {
        None,
        BuildBigHome,   // 盖一座大房子
        BecomeRich,     // 成为最富有的人
        GainRespect,    // 得到大家的敬重
        ProtectColony,  // 守护家园
        WanderWorld,    // 走遍世界
        MasterCraft,    // 成为手艺大师
        FindFamily,     // 找到归属/家人
    }

    /// <summary>对玩家命令的接受意愿。</summary>
    [Serializable]
    public enum CommandAcceptance { Accept, Delay, Refuse }

    /// <summary>
    /// 一条事件记忆（episodic memory）— 记录"发生过什么"供心智层引用。
    /// Weight 逐日衰减，低于剪枝阈值被遗忘剔除。
    /// </summary>
    [Serializable]
    public class WorkerMemoryEntry
    {
        /// <summary>事件发生的游戏日。</summary>
        public int Day;

        /// <summary>归一化事件类型键（WorkerMindConstant.EVT_*）。</summary>
        public string TypeKey;

        /// <summary>事件正负向。</summary>
        public MemoryValence Valence;

        /// <summary>相关目标："PLAYER" 哨兵或 Worker 稳定名；空 = 无目标。</summary>
        public string TargetName;

        /// <summary>强度 0-100。</summary>
        public float Intensity;

        /// <summary>残留权重，逐日衰减，低于剪枝阈值剔除。</summary>
        public float Weight = 1f;

        /// <summary>记仇类事件是否已化解。</summary>
        public bool Resolved;
    }

    /// <summary>与一个目标 Worker 的关系记录。</summary>
    [Serializable]
    public class WorkerRelationEntry
    {
        /// <summary>目标 Worker 稳定名。</summary>
        public string TargetName;

        /// <summary>关系类型。</summary>
        public RelationKind Kind = RelationKind.None;

        /// <summary>长期亲密度 -100..100（非好感度，低频定性）。</summary>
        public float Affinity;

        /// <summary>记仇 0-100。</summary>
        public float GrudgeLevel;

        /// <summary>爱慕/崇拜 0-100。</summary>
        public float AdmirationLevel;

        /// <summary>最近交互游戏日。</summary>
        public int LastInteractionDay;

        /// <summary>与该目标的关键记忆（上限 5）。</summary>
        public List<WorkerMemoryEntry> KeyMemories = new List<WorkerMemoryEntry>();
    }

    /// <summary>人格漂移累积量（四桶，累积到阈值一次性迁移到 Personality）。</summary>
    [Serializable]
    public class PersonalityDrift
    {
        public float MoodDrift;
        public float AmbitionDrift;
        public float DiligenceDrift;
        public float SocialityDrift;

        // 滞回带方向记忆：0=从未迁移，+1=上次正向迁移，-1=上次反向迁移。
        // 迁移后反向需再积 (Threshold + Band) 才回迁（防反复横跳）。
        public int MoodDir;
        public int AmbitionDir;
        public int DiligenceDir;
        public int SocialityDir;
    }

    /// <summary>个人愿望/执念 — 超越 4 类固定目标（WorkerGoal）的个人梦想。</summary>
    [Serializable]
    public struct WorkerDream
    {
        public WorkerDreamType Type;
        public string Description;
        public int BornDay;
        public int LastPursuedDay;
        public float Passion;

        /// <summary>无执念。</summary>
        public bool IsEmpty => this.Type == WorkerDreamType.None;

        public static WorkerDream None => new WorkerDream { Type = WorkerDreamType.None };
    }

    /// <summary>
    /// Worker 心智层总容器 — 作为 WorkerData.Mind 的单引用字段。
    /// 全部字段 [Serializable] 纯值类型，随角色二进制存档（BinaryFormatter）一次写入。
    /// 跨存档引用一律用 Name 字符串 + "PLAYER" 哨兵，集合用 List（不用 Dictionary）。
    /// </summary>
    [Serializable]
    public class WorkerMindData
    {
        // ---- 信念 [0,100]，初始 50 ----
        /// <summary>对世界的安全/公平感。</summary>
        public float TrustInWorld = 50f;

        /// <summary>对玩家指挥的信任。</summary>
        public float TrustInPlayer = 50f;

        /// <summary>自我价值感。</summary>
        public float SelfEsteem = 50f;

        /// <summary>归属感。</summary>
        public float SenseOfBelonging = 50f;

        // ---- 对玩家的自主意志状态（与好感度区分）----
        /// <summary>服从意愿 0-100 — 主观上愿不愿意听玩家指挥。</summary>
        public float WillingnessToObey = 50f;

        /// <summary>对玩家的怨恨 0-100 — 积怨越深越拒绝玩家命令。</summary>
        public float ResentmentToPlayer = 0f;

        /// <summary>对玩家的感恩 0-100 — 被救危等提升，可覆盖怨恨接单。</summary>
        public float GratitudeToPlayer = 0f;

        // ---- 事件记忆 ----
        public List<WorkerMemoryEntry> Memories = new List<WorkerMemoryEntry>();
        public int MemoryCap = 24;

        // ---- 愿望/执念 ----
        public WorkerDream ActiveDream = WorkerDream.None;
        public List<WorkerDream> DreamHistory = new List<WorkerDream>();

        // ---- 随机人生事件 ----
        public int LastLifeEventDay = -1;
        public int LifeEventCount = 0;
        public List<WorkerMemoryEntry> LifeEventHistory = new List<WorkerMemoryEntry>();

        // ---- 人格漂移 ----
        public PersonalityDrift Drift = new PersonalityDrift();

        // ---- 关系 ----
        public List<WorkerRelationEntry> Relations = new List<WorkerRelationEntry>();

        // ---- 命令/拖延/强制 ----
        /// <summary>最近一次拒绝玩家命令的时间（Time.time），用于拖延冷却。</summary>
        public float LastPlayerCommandRefusalTime = -999f;

        /// <summary>拒绝玩家悬赏次数。</summary>
        public int RefusedPlayerBountyCount = 0;

        /// <summary>接受玩家悬赏次数。</summary>
        public int AcceptedPlayerBountyCount = 0;

        /// <summary>强制命令放行窗口截止时间（Time.time）。窗口内不拒绝玩家命令。</summary>
        public float ForcedUntilTime = -999f;

        /// <summary>被强制命令次数。</summary>
        public int ForcedCommandCount = 0;

        /// <summary>最近一次强制命令时间（Time.time）。-999 = 从未强制。防连击门。</summary>
        public float LastForceCommandTime = -999f;

        /// <summary>怨恨/感恩自然消退的上次时间（Time.time）。</summary>
        public float LastResentmentDecayTime = -999f;

        /// <summary>最近一次濒死经历的游戏日（按日节流，避免同一天刷屏记忆）。-1 = 从未。</summary>
        public int LastNearDeathDay = -1;

        /// <summary>最近一次人格迁移的游戏日（限流）。</summary>
        public float LastDriftMigrationDay = -1f;

        /// <summary>
        /// 读档兜底：BinaryFormatter 反序列化不运行构造函数，老档的 Mind 为 null。
        /// 所有读取路径在操作 Mind 前调用此方法。
        /// </summary>
        public static void Ensure(AWorker.WorkerData wd)
        {
            if (wd != null && wd.Mind == null)
            {
                wd.Mind = new WorkerMindData();
            }
        }
    }
}
