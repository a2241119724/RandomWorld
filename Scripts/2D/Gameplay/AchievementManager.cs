namespace LAB2D.Gameplay
{
    using LAB2D;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Gameplay;
    using System;
    using System.Collections.Generic;
    using System.Text;
    /// <summary>
    /// 成就管理器 — 管理所有成就的定义、进度跟踪、条件检测和解锁通知。
    /// 单例，在 GlobalInit.Start 中初始化。
    ///
    /// 接入方式：
    ///   1. 调用 AchievementManager.Instance.Initialize() 初始化所有成就定义
    ///   2. 每帧或事件驱动的 UpdateProgressAll() 更新所有成就进度
    ///   3. 订阅 OnAchievementUnlocked 事件获取解锁通知
    ///   4. AchievementPopup / AchievementPanel 只读查询成就列表
    ///
    /// 风险限制：
    ///   不直接修改任何战斗/关卡/物品逻辑，只读取已有统计数据。
    ///   不涉及 Photon 同步和存档格式修改（存档扩展由后续需求覆盖）。
    /// </summary>
    public class AchievementManager : Singleton<AchievementManager>, IInitializable, ITickable
    {
        /// <summary>所有成就定义列表（按类别分组）</summary>
        private readonly List<AchievementData> allAchievements = new List<AchievementData>();

        /// <summary>按 ID 快速查找成就</summary>
        private readonly Dictionary<string, AchievementData> achievementById = new Dictionary<string, AchievementData>();

        /// <summary>待弹窗展示的解锁成就队列</summary>
        private readonly Queue<AchievementData> pendingUnlockQueue = new Queue<AchievementData>();

        private readonly AchievementRuleService ruleService = new AchievementRuleService();

        /// <summary>总成就点数（所有已解锁成就的点数和）</summary>
        private int totalPointsEarned;

        /// <summary>是否已初始化</summary>
        private bool initialized;
        private IGameTime gameTime;
        private IGameLogger gameLogger;

        private IGameTime GameTime => this.gameTime ?? (this.gameTime = Core.ServiceLocator.Get<IGameTime>());
        private IGameLogger GameLogger => this.gameLogger ?? (this.gameLogger = Core.ServiceLocator.Get<IGameLogger>());

        /// <summary>成就解锁事件，参数为解锁的成就数据</summary>
        public event Action<AchievementData> OnAchievementUnlocked;

        /// <summary>成就进度更新事件</summary>
        public event Action OnProgressUpdated;

        // --- 公开查询属性 ---

        /// <summary>所有成就（只读副本）</summary>
        public IReadOnlyList<AchievementData> AllAchievements
        {
            get { return this.allAchievements; }
        }

        /// <summary>已解锁成就列表</summary>
        public List<AchievementData> UnlockedAchievements
        {
            get
            {
                List<AchievementData> list = new List<AchievementData>();
                foreach (AchievementData a in this.allAchievements)
                {
                    if (a.State == AchievementState.Unlocked || a.State == AchievementState.Claimed)
                    {
                        list.Add(a);
                    }
                }

                return list;
            }
        }

        /// <summary>成就总数</summary>
        public int TotalCount
        {
            get { return this.allAchievements.Count; }
        }

        /// <summary>已解锁成就数</summary>
        public int UnlockedCount
        {
            get { return this.UnlockedAchievements.Count; }
        }

        /// <summary>已获得成就点数</summary>
        public int TotalPointsEarned
        {
            get { return this.totalPointsEarned; }
        }

        /// <summary>待展示解锁弹窗队列中是否有新解锁成就</summary>
        public bool HasPendingUnlock
        {
            get { return this.pendingUnlockQueue.Count > 0; }
        }

        /// <summary>是否已初始化</summary>
        public bool IsInitialized
        {
            get { return this.initialized; }
        }

        /// <summary>
        /// 初始化所有成就定义。
        /// 多次调用安全：已初始化时直接返回。
        /// </summary>
        public void Initialize()
        {
            if (this.initialized)
            {
                return;
            }

            this.initialized = true;
            this.RegisterAchievements();
            this.SyncProgressFromStats();
        }

        /// <summary>
        /// 每帧更新成就进度并展示待解锁弹窗。
        /// 由 GlobalInit 通过 ITickable 接口统一驱动。
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!this.initialized)
            {
                return;
            }

            this.UpdateProgressAll();

            if (this.HasPendingUnlock)
            {
                AchievementData pending = this.PeekPendingUnlock();
                if (pending != null && AchievementPopup.RuntimeInstance != null)
                {
                    AchievementPopup.RuntimeInstance.Show(pending);
                }
            }
        }

        /// <summary>
        /// 获取下一个待展示的解锁成就（不移除，用于弹窗展示）
        /// </summary>
        /// <returns>待展示成就，队列为空返回 null</returns>
        public AchievementData PeekPendingUnlock()
        {
            if (this.pendingUnlockQueue.Count == 0)
            {
                return null;
            }

            return this.pendingUnlockQueue.Peek();
        }

        /// <summary>
        /// 移除并返回下一个待展示解锁成就（弹窗关闭后调用）
        /// </summary>
        /// <returns>已移除的成就</returns>
        public AchievementData DequeuePendingUnlock()
        {
            if (this.pendingUnlockQueue.Count == 0)
            {
                return null;
            }

            return this.pendingUnlockQueue.Dequeue();
        }

        /// <summary>
        /// 将指定成就标记为已领取
        /// </summary>
        /// <param name="id">成就 ID</param>
        public void ClaimAchievement(string id)
        {
            if (this.achievementById.TryGetValue(id, out AchievementData data))
            {
                if (data.State == AchievementState.Unlocked)
                {
                    data.State = AchievementState.Claimed;
                }
            }
        }

        /// <summary>
        /// 将所有已解锁成就标记为已领取
        /// </summary>
        public void ClaimAllUnlocked()
        {
            foreach (AchievementData data in this.allAchievements)
            {
                if (data.State == AchievementState.Unlocked)
                {
                    data.State = AchievementState.Claimed;
                }
            }
        }

        /// <summary>
        /// 更新所有成就进度（每帧或按需调用）。
        /// 从 GameplaySessionStats 等已有管理器读取最新统计值，更新匹配的成就进度。
        /// </summary>
        public void UpdateProgressAll()
        {
            if (!this.initialized)
            {
                return;
            }

            bool anyChanged = false;

            // 从 GameplaySessionStats 快照读取会话统计数据
            GameplaySessionStats stats = Core.ServiceLocator.Get<GameplaySessionStats>();
            GameplaySessionStatsSnapshot snap = stats?.CreateSnapshot();
            if (snap != null)
            {
                anyChanged |= this.UpdateAchievement("combat_kill_1", snap.TotalDefeatedEnemyCount);
                anyChanged |= this.UpdateAchievement("combat_kill_100", snap.TotalDefeatedEnemyCount);
                anyChanged |= this.UpdateAchievement("combat_kill_1000", snap.TotalDefeatedEnemyCount);
                anyChanged |= this.UpdateAchievement("combat_combo_50", snap.MaxCombo);
                anyChanged |= this.UpdateAchievement("combat_crit_100", snap.CriticalHitCount);
                anyChanged |= this.UpdateAchievement("combat_boss_10", snap.TotalDefeatedEnemyCount);
                anyChanged |= this.UpdateAchievement("survival_exp_10", snap.TotalExperienceGained);
                anyChanged |= this.UpdateAchievement("collection_item_100", snap.TotalCollectedItemCount);
                anyChanged |= this.UpdateAchievement("collection_item_1000", snap.TotalCollectedItemCount);
                anyChanged |= this.UpdateAchievement("collection_item_10000", snap.TotalCollectedItemCount);
            }

            // 波次类
            WaveManager wave = Core.ServiceLocator.Get<WaveManager>();
            if (wave != null)
            {
                anyChanged |= this.UpdateAchievement("wave_complete_1", wave.TotalWavesCompleted);
                anyChanged |= this.UpdateAchievement("wave_complete_10", wave.TotalWavesCompleted);
                anyChanged |= this.UpdateAchievement("wave_complete_50", wave.TotalWavesCompleted);
            }

            // 工人运营类
            WorkerEfficiencyTracker efficiency = Core.ServiceLocator.Get<WorkerEfficiencyTracker>();
            if (efficiency != null)
            {
                anyChanged |= this.UpdateAchievement("worker_task_10", efficiency.TotalTasksCompleted);
                anyChanged |= this.UpdateAchievement("worker_task_100", efficiency.TotalTasksCompleted);
                anyChanged |= this.UpdateAchievement("worker_task_500", efficiency.TotalTasksCompleted);
            }

            // 生存 - 死亡次数（从快照读取，已累计至当前会话）
            if (snap != null)
            {
                anyChanged |= this.UpdateAchievement("survival_death_1", snap.PlayerDeathCount);
            }

            // 生存 - 存活时间（从会话开始时间计算）
            float sessionTime = this.GameTime.Time;
            int minutes = this.ruleService.GetElapsedMinutes(sessionTime);
            anyChanged |= this.UpdateAchievement("survival_time_30", minutes);

            // 生存 - 等级（从玩家数据读取）
            Player player = Core.ServiceLocator.TryGet(out PlayerManager pm) ? pm.Mine : null;
            if (player != null && player.CharacterDataLAB != null)
            {
                anyChanged |= this.UpdateAchievement("survival_level_50", player.CharacterDataLAB.Level);
            }

            if (anyChanged)
            {
                this.OnProgressUpdated?.Invoke();
            }
        }

        /// <summary>
        /// 从统计数据同步初始进度（初始化时调用）。
        /// </summary>
        private void SyncProgressFromStats()
        {
            this.UpdateProgressAll();
        }

        /// <summary>
        /// 更新单个成就进度。如果成就不存在、已解锁、进度不变则跳过。
        /// </summary>
        /// <param name="id">成就 ID</param>
        /// <param name="newProgress">新的进度值</param>
        /// <returns>是否有变化</returns>
        private bool UpdateAchievement(string id, int newProgress)
        {
            if (!this.achievementById.TryGetValue(id, out AchievementData data))
            {
                return false;
            }

            // 已解锁或已领取则不再更新
            if (data.State == AchievementState.Unlocked || data.State == AchievementState.Claimed)
            {
                return false;
            }

            // 限制进度不超过目标值
            int clamped = this.ruleService.ClampProgressToTarget(newProgress, data.TargetValue);
            if (clamped == data.CurrentProgress)
            {
                return false;
            }

            data.CurrentProgress = clamped;

            // 检查是否达成目标
            if (data.IsTargetReached && data.State == AchievementState.Locked)
            {
                this.UnlockAchievement(data);
            }

            return true;
        }

        /// <summary>
        /// 解锁成就：设置状态、加入展示队列、累加点数、触发事件。
        /// </summary>
        /// <param name="data">要解锁的成就</param>
        private void UnlockAchievement(AchievementData data)
        {
            data.State = AchievementState.Unlocked;
            this.totalPointsEarned += data.Points;
            this.pendingUnlockQueue.Enqueue(data);
            this.OnAchievementUnlocked?.Invoke(data);
            this.GameLogger.Log($"[成就系统] 解锁成就: {data.Name} (+{data.Points}点)");
        }

        /// <summary>
        /// 注册所有成就定义。
        /// 按类别分组定义，每个成就有唯一 ID、名称、描述、类别、目标值、点数。
        /// </summary>
        private void RegisterAchievements()
        {
            this.allAchievements.Clear();
            this.achievementById.Clear();

            // ===== 战斗类 =====
            this.AddAchievement(new AchievementData
            {
                Id = "combat_kill_1",
                Name = "初出茅庐",
                Description = "首次击败一个敌人",
                Category = AchievementCategory.Combat,
                ConditionTemplate = "击杀 {0} 个敌人",
                TargetValue = 1,
                Points = 10,
                IsProgressPersistent = true,
                State = AchievementState.Locked,
            });

            this.AddAchievement(new AchievementData
            {
                Id = "combat_kill_100",
                Name = "百人斩",
                Description = "累计击败100个敌人",
                Category = AchievementCategory.Combat,
                ConditionTemplate = "累计击杀 {0} 个敌人",
                TargetValue = 100,
                Points = 30,
                IsProgressPersistent = true,
                State = AchievementState.Locked,
            });

            this.AddAchievement(new AchievementData
            {
                Id = "combat_kill_1000",
                Name = "千人斩",
                Description = "累计击败1000个敌人",
                Category = AchievementCategory.Combat,
                ConditionTemplate = "累计击杀 {0} 个敌人",
                TargetValue = 1000,
                Points = 100,
                IsProgressPersistent = true,
                State = AchievementState.Locked,
            });

            this.AddAchievement(new AchievementData
            {
                Id = "combat_combo_50",
                Name = "连击大师",
                Description = "在一次连击中击败50个敌人",
                Category = AchievementCategory.Combat,
                ConditionTemplate = "最高连击数达到 {0}",
                TargetValue = 50,
                Points = 50,
                IsProgressPersistent = false,
                State = AchievementState.Locked,
            });

            this.AddAchievement(new AchievementData
            {
                Id = "combat_crit_100",
                Name = "暴击达人",
                Description = "累计造成100次暴击",
                Category = AchievementCategory.Combat,
                ConditionTemplate = "累计暴击 {0} 次",
                TargetValue = 100,
                Points = 30,
                IsProgressPersistent = true,
                State = AchievementState.Locked,
            });

            this.AddAchievement(new AchievementData
            {
                Id = "combat_boss_10",
                Name = "Boss猎手",
                Description = "击败10只Boss级敌人",
                Category = AchievementCategory.Combat,
                ConditionTemplate = "击败 {0} 只Boss",
                TargetValue = 10,
                Points = 60,
                IsProgressPersistent = true,
                State = AchievementState.Locked,
            });

            // ===== 收集类 =====
            this.AddAchievement(new AchievementData
            {
                Id = "collection_item_100",
                Name = "收集新手",
                Description = "累计收集100个物品",
                Category = AchievementCategory.Collection,
                ConditionTemplate = "累计收集 {0} 个物品",
                TargetValue = 100,
                Points = 20,
                IsProgressPersistent = true,
                State = AchievementState.Locked,
            });

            this.AddAchievement(new AchievementData
            {
                Id = "collection_item_1000",
                Name = "收集达人",
                Description = "累计收集1000个物品",
                Category = AchievementCategory.Collection,
                ConditionTemplate = "累计收集 {0} 个物品",
                TargetValue = 1000,
                Points = 50,
                IsProgressPersistent = true,
                State = AchievementState.Locked,
            });

            this.AddAchievement(new AchievementData
            {
                Id = "collection_item_10000",
                Name = "收集大师",
                Description = "累计收集10000个物品",
                Category = AchievementCategory.Collection,
                ConditionTemplate = "累计收集 {0} 个物品",
                TargetValue = 10000,
                Points = 150,
                IsProgressPersistent = true,
                State = AchievementState.Locked,
            });

            // ===== 生存类 =====
            this.AddAchievement(new AchievementData
            {
                Id = "survival_death_1",
                Name = "初尝败绩",
                Description = "首次死亡",
                Category = AchievementCategory.Survival,
                ConditionTemplate = "累计死亡 {0} 次",
                TargetValue = 1,
                Points = 10,
                IsProgressPersistent = true,
                State = AchievementState.Locked,
            });

            this.AddAchievement(new AchievementData
            {
                Id = "survival_time_30",
                Name = "持久战",
                Description = "单局存活超过30分钟",
                Category = AchievementCategory.Survival,
                ConditionTemplate = "单局存活 {0} 分钟",
                TargetValue = 30,
                Points = 40,
                IsProgressPersistent = false,
                State = AchievementState.Locked,
            });

            this.AddAchievement(new AchievementData
            {
                Id = "survival_exp_10",
                Name = "经验丰富",
                Description = "累计获得10000点经验",
                Category = AchievementCategory.Survival,
                ConditionTemplate = "累计获得 {0} 点经验",
                TargetValue = 10000,
                Points = 30,
                IsProgressPersistent = true,
                State = AchievementState.Locked,
            });

            this.AddAchievement(new AchievementData
            {
                Id = "survival_level_50",
                Name = "登峰造极",
                Description = "角色等级达到50级",
                Category = AchievementCategory.Survival,
                ConditionTemplate = "角色等级达到 {0} 级",
                TargetValue = 50,
                Points = 100,
                IsProgressPersistent = false,
                State = AchievementState.Locked,
            });

            // ===== 波次类 =====
            this.AddAchievement(new AchievementData
            {
                Id = "wave_complete_1",
                Name = "首次通关",
                Description = "完成第1波敌人",
                Category = AchievementCategory.Wave,
                ConditionTemplate = "完成 {0} 波敌人",
                TargetValue = 1,
                Points = 10,
                IsProgressPersistent = true,
                State = AchievementState.Locked,
            });

            this.AddAchievement(new AchievementData
            {
                Id = "wave_complete_10",
                Name = "波次专家",
                Description = "累计完成10波敌人",
                Category = AchievementCategory.Wave,
                ConditionTemplate = "累计完成 {0} 波敌人",
                TargetValue = 10,
                Points = 40,
                IsProgressPersistent = true,
                State = AchievementState.Locked,
            });

            this.AddAchievement(new AchievementData
            {
                Id = "wave_complete_50",
                Name = "波次大师",
                Description = "累计完成50波敌人",
                Category = AchievementCategory.Wave,
                ConditionTemplate = "累计完成 {0} 波敌人",
                TargetValue = 50,
                Points = 120,
                IsProgressPersistent = true,
                State = AchievementState.Locked,
            });

            // ===== 工人运营类 =====
            this.AddAchievement(new AchievementData
            {
                Id = "worker_task_10",
                Name = "勤劳蚂蚁",
                Description = "工人累计完成10个任务",
                Category = AchievementCategory.Worker,
                ConditionTemplate = "工人累计完成 {0} 个任务",
                TargetValue = 10,
                Points = 20,
                IsProgressPersistent = true,
                State = AchievementState.Locked,
            });

            this.AddAchievement(new AchievementData
            {
                Id = "worker_task_100",
                Name = "勤劳蜜蜂",
                Description = "工人累计完成100个任务",
                Category = AchievementCategory.Worker,
                ConditionTemplate = "工人累计完成 {0} 个任务",
                TargetValue = 100,
                Points = 50,
                IsProgressPersistent = true,
                State = AchievementState.Locked,
            });

            this.AddAchievement(new AchievementData
            {
                Id = "worker_task_500",
                Name = "殖民地大师",
                Description = "工人累计完成500个任务",
                Category = AchievementCategory.Worker,
                ConditionTemplate = "工人累计完成 {0} 个任务",
                TargetValue = 500,
                Points = 150,
                IsProgressPersistent = true,
                State = AchievementState.Locked,
            });
        }

        /// <summary>
        /// 向管理器中添加成就定义
        /// </summary>
        /// <param name="data">成就数据</param>
        private void AddAchievement(AchievementData data)
        {
            if (data == null || string.IsNullOrEmpty(data.Id))
            {
                return;
            }

            this.allAchievements.Add(data);
            this.achievementById[data.Id] = data;
        }

        /// <summary>
        /// 构建完整的成就状态摘要文本（供 Debug 或 报告使用）
        /// </summary>
        /// <returns>成就摘要文本</returns>
        public string BuildFullSummaryText()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== 成就系统状态 ===");
            sb.AppendLine($"总计: {this.UnlockedCount}/{this.TotalCount} 已解锁");
            sb.AppendLine($"成就点数: {this.totalPointsEarned}");

            AchievementCategory currentCategory = (AchievementCategory)(-1);
            foreach (AchievementData data in this.allAchievements)
            {
                if (data.Category != currentCategory)
                {
                    currentCategory = data.Category;
                    sb.AppendLine();
                    sb.AppendLine($"[{data.CategoryDisplayName}]");
                }

                string stateIcon = data.State == AchievementState.Claimed ? "[V]" :
                                   data.State == AchievementState.Unlocked ? "[!]" : "[ ]";
                sb.AppendLine($"  {stateIcon} {data.Name} - {data.ProgressText} ({data.PointsText})");
            }

            return sb.ToString();
        }
    }
}
