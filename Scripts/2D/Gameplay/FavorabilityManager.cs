namespace LAB2D.Gameplay
{
    using LAB2D.AI.Dialogue.Prompt;
    using LAB2D.Character.Player;
    using LAB2D.Character.Worker;
    using LAB2D.Domain.Common;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 好感度管理器 — Worker 之间、Worker 与 Player 之间的定向好感数值。
    /// 仿 CurrencyManager：ASingletonSaveData 提供单例 + 二进制存档，ITickable 驱动接近扫描。
    /// 运行时键用 Worker.GetInstanceID()，Player 恒为 0（与 PlayerBountyService.PlayerOwnerId 一致）。
    /// 存档按 Name 稳定匹配（BuildMap.BuilderName 先例），旧档无此文件时全部默认 50，零迁移。
    /// </summary>
    public class FavorabilityManager : ASingletonSaveData<FavorabilityManager>, ITickable
    {
        /// <summary>Player 在好感度系统中的恒等 id。</summary>
        public const int PlayerId = 0;

        /// <summary>存档中表示"对玩家"的 TargetName 哨兵。</summary>
        private const string PlayerTargetName = "PLAYER";

        /// <summary>好感度变化事件 — UI 订阅刷新。参数：(holder Worker, 变化对象 id(0=Player)，变化增量)。</summary>
        public event Action<AWorker, int, float> OnFavorabilityChanged;

        /// <summary>每个 Worker 的定向好感档案。key = Worker.GetInstanceID()。</summary>
        private readonly Dictionary<int, FavorabilityProfile> profiles = new Dictionary<int, FavorabilityProfile>();

        /// <summary>接近/共事累计增益 — key = (from&lt;&lt;32|to)，value = 已累计好感（防每对刷满）。</summary>
        private readonly Dictionary<long, float> proximityGains = new Dictionary<long, float>();

        /// <summary>Player 救危加分冷却 — key = Worker instanceID，value = 最近一次获得救危好感的时间。</summary>
        private readonly Dictionary<int, float> playerHelpCooldowns = new Dictionary<int, float>();

        private float proximityTimer;

        // ---- 摘要文本构建复用容器（HUD 每 0.5s 一次 × 上百 Worker，不能每次新建）----

        /// <summary>BuildSummaryText 复用构建器，避免每次新建 StringBuilder 及其中间扩容数组。</summary>
        private readonly System.Text.StringBuilder summaryBuilder = new System.Text.StringBuilder(4096);

        /// <summary>BuildSummaryText 复用关系列表（Top 3 排序用）。</summary>
        private readonly List<(AWorker From, AWorker To, float Value)> topRelations = new List<(AWorker From, AWorker To, float Value)>(64);

        /// <summary>BuildSummaryText 复用 id→Worker 映射：替代对每条关系做 FindWorker 线性扫描。</summary>
        private readonly Dictionary<int, AWorker> summaryIdToWorker = new Dictionary<int, AWorker>(64);

        /// <summary>
        /// 单个 Worker 的好感档案。
        /// toWorkers 懒初始化：首次查询未命中返回默认 50，避免 N² 预填。
        /// </summary>
        private sealed class FavorabilityProfile
        {
            public float toPlayer = FavorabilityRuleService.InitialFavorability;
            public Dictionary<int, float> toWorkers;
            public int talkCountToday;
            public int lastTalkGameDay;
        }

        // ---- 初始化 ----

        /// <summary>Worker 生成时登记好感档案（WorkerManager.Add 调用）。初始好感取 Worker profile 配置，默认 50。</summary>
        public void InitializeWorkerFavorability(AWorker worker)
        {
            if (worker == null) return;
            int id = worker.GetInstanceID();
            if (this.profiles.ContainsKey(id)) return; // 读档后已有，跳过

            float init = FavorabilityRuleService.InitialFavorability;
            NPCPromptProfile profile = Core.ServiceLocator.Get<PromptBuilder>()?.GetProfile("Worker");
            if (profile != null)
            {
                init = FavorabilityRuleService.Clamp(profile.initialFavorability);
            }

            this.profiles[id] = new FavorabilityProfile { toPlayer = init };
            AWorkerTask.LogProvider($"[Favor] {worker.name} 登记好感档案，对玩家初始 {init:F0}", LogManager.LogLevelEnum.Debug);
        }

        // ---- 查询 ----

        /// <summary>from 对 toId（0=Player，&gt;0=Worker instanceID）的好感。未建立关系返回默认 50。</summary>
        public float GetFavorability(AWorker from, int toId)
        {
            if (from == null) return FavorabilityRuleService.InitialFavorability;
            if (!this.profiles.TryGetValue(from.GetInstanceID(), out FavorabilityProfile p)) return FavorabilityRuleService.InitialFavorability;
            if (toId == PlayerId) return p.toPlayer;
            if (p.toWorkers == null || !p.toWorkers.TryGetValue(toId, out float v)) return FavorabilityRuleService.InitialFavorability;
            return v;
        }

        /// <summary>from 对 Player 的好感。</summary>
        public float GetFavorabilityWithPlayer(AWorker from)
        {
            return this.GetFavorability(from, PlayerId);
        }

        /// <summary>from 对 to Worker 的好感。</summary>
        public float GetFavorability(AWorker from, AWorker to)
        {
            if (to == null) return FavorabilityRuleService.InitialFavorability;
            return this.GetFavorability(from, to.GetInstanceID());
        }

        /// <summary>from 对玩家的态度标签（&lt;30 敌对 / 30-49 疏远 / 50-69 友好 / 70-84 亲近 / ≥85 挚友）。</summary>
        public string GetAttitudeLabel(AWorker from)
        {
            return FavorabilityRuleService.GetAttitudeLabel(this.GetFavorabilityWithPlayer(from));
        }

        /// <summary>好感度摘要文本（HUD 用）：每名 Worker 对玩家好感+态度标签，附最强 3 条 Worker↔Worker 关系。
        /// 复用 builder/列表/id 映射（HUD 每 0.5s 调用一次，原实现对每条关系做 FindWorker 线性扫描，
        /// 100 Worker × 数百关系 = 每 0.5s 数万次比较 + 多次字符串分配，为帧率热点）。</summary>
        public string BuildSummaryText()
        {
            WorkerManager wm = Core.ServiceLocator.Get<WorkerManager>();
            if (wm == null) return string.Empty;

            List<AWorker> workers = wm.Characters;

            // id→Worker 一次构建，关系遍历改为 O(1) 查表
            Dictionary<int, AWorker> idToWorker = this.summaryIdToWorker;
            idToWorker.Clear();
            foreach (AWorker w in workers)
            {
                if (w != null) idToWorker[w.GetInstanceID()] = w;
            }

            System.Text.StringBuilder sb = this.summaryBuilder;
            sb.Clear();
            sb.AppendLine("━━━ 好感度 ━━━");
            sb.AppendLine("· 对玩家");

            foreach (AWorker w in workers)
            {
                if (w == null) continue;
                sb.Append("  ");
                sb.Append(w.name);
                sb.Append("：");
                AppendRounded(sb, this.GetFavorabilityWithPlayer(w));
                sb.Append("（");
                sb.Append(this.GetAttitudeLabel(w));
                sb.Append("）");
                sb.Append('\n');
            }

            // 最强 Worker↔Worker 关系（Top 3）
            List<(AWorker From, AWorker To, float Value)> relations = this.topRelations;
            relations.Clear();
            foreach (KeyValuePair<int, FavorabilityProfile> kv in this.profiles)
            {
                if (kv.Value.toWorkers == null) continue;
                if (!idToWorker.TryGetValue(kv.Key, out AWorker holder)) continue;
                foreach (KeyValuePair<int, float> wk in kv.Value.toWorkers)
                {
                    if (!idToWorker.TryGetValue(wk.Key, out AWorker target)) continue;
                    relations.Add((holder, target, wk.Value));
                }
            }
            relations.Sort((a, b) => b.Value.CompareTo(a.Value));

            sb.Append("· Worker 间亲近关系");
            if (relations.Count > 0)
            {
                sb.Append('\n');
                for (int i = 0; i < Math.Min(3, relations.Count); i++)
                {
                    (AWorker from, AWorker to, float value) = relations[i];
                    sb.Append("  ");
                    sb.Append(from.name);
                    sb.Append(" ↔ ");
                    sb.Append(to.name);
                    sb.Append("：");
                    AppendRounded(sb, value);
                    sb.Append('\n');
                }
            }
            else
            {
                sb.Append('\n');
                sb.AppendLine("  （暂无——一起工作/交易/互助会逐步建立）");
            }

            return sb.ToString();
        }

        /// <summary>按 &quot;F0&quot; 语义（四舍五入到整数）追加数字，避免 float.ToString 的每次格式化分配。
        /// 好感度经 Clamp 恒非负，+0.5 截断即四舍五入。</summary>
        private static void AppendRounded(System.Text.StringBuilder sb, float value)
        {
            sb.Append((int)(value + 0.5f));
        }

        // ---- 修改 ----

        /// <summary>修改 from 对 toId 的好感。delta 可为负。联动 Mood，发布变化事件。</summary>
        public void ModifyFavorability(AWorker from, int toId, float delta, string reason)
        {
            if (from == null || delta == 0f) return;
            int fromId = from.GetInstanceID();
            FavorabilityProfile p = this.GetOrCreateProfile(fromId);

            float newValue;
            if (toId == PlayerId)
            {
                p.toPlayer = FavorabilityRuleService.Clamp(p.toPlayer + delta);
                newValue = p.toPlayer;
            }
            else
            {
                p.toWorkers ??= new Dictionary<int, float>();
                float cur = p.toWorkers.TryGetValue(toId, out float v) ? v : FavorabilityRuleService.InitialFavorability;
                p.toWorkers[toId] = FavorabilityRuleService.Clamp(cur + delta);
                newValue = p.toWorkers[toId];
            }

            this.OnFavorabilityChanged?.Invoke(from, toId, delta);
            this.ApplyMoodDelta(from, delta);
            AWorkerTask.LogProvider(
                $"[Favor] {from.name} → {(toId == PlayerId ? "玩家" : $"Worker[{toId}]")} 好感 {newValue:F0} ({reason}, {delta:+#.##;-#.##;0})",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>修改 from 对 Player 的好感。</summary>
        public void ModifyWithPlayer(AWorker from, float delta, string reason)
        {
            this.ModifyFavorability(from, PlayerId, delta, reason);
        }

        /// <summary>救危通知的邻近 Worker 复用缓冲（单线程 ITickable/受击路径，无重入）。</summary>
        private readonly List<AWorker> nearbyWorkersBuffer = new List<AWorker>();

        /// <summary>
        /// Player 救危：Player 在敌方受击点附近击伤敌人 → 附近 Worker 对玩家好感上升。
        /// 启发式：以受击点为中心、ProximityRadiusMapTiles 半径，30s 冷却/Worker。
        /// 由 Character.ReduceHp 基类在"Player 攻击非玩家非 Worker"时调用。
        /// 走 WorkerManager 空间网格（O(邻近 Worker 数)，替代原全列表扫描，行为不变）。
        /// </summary>
        public void NotifyPlayerHelpsNearby(float worldX, float worldY)
        {
            WorkerManager wm = Core.ServiceLocator.Get<WorkerManager>();
            if (wm == null) return;

            float now = Time.time;

            wm.EnsureWorkerGridRebuilt();
            this.nearbyWorkersBuffer.Clear();
            wm.WorkerGrid.QueryRange(
                new GameVector2(worldX, worldY),
                FavorabilityConstant.ProximityRadiusMapTiles,
                this.nearbyWorkersBuffer,
                w => w != null);

            foreach (AWorker w in this.nearbyWorkersBuffer)
            {
                int id = w.GetInstanceID();
                if (this.playerHelpCooldowns.TryGetValue(id, out float last)
                    && (now - last) < FavorabilityConstant.HelpCoolDownSeconds)
                {
                    continue;
                }

                this.playerHelpCooldowns[id] = now;
                this.ModifyWithPlayer(w, FavorabilityConstant.HelpVsEnemyDelta, "Player 救危");

                // 心智层钩子：玩家救危同时提升感恩/信任（EVT_PLAYER_HELP 事件在 Phase 2 全量接入）
                if (Core.ServiceLocator.TryGet<WorkerMindService>(out WorkerMindService mindService))
                {
                    mindService.RecordPlayerHelp(w);
                }
            }
        }

        // ---- 对话 ----

        /// <summary>对话结束时调用：好感 +2，每日上限 10（按游戏日重置）。</summary>
        public void NotifyConversationEnd(AWorker worker)
        {
            if (worker == null) return;
            FavorabilityProfile p = this.GetOrCreateProfile(worker.GetInstanceID());

            int day = this.GetGameDayIndex();
            if (p.lastTalkGameDay != day)
            {
                p.lastTalkGameDay = day;
                p.talkCountToday = 0;
            }

            if (!FavorabilityRuleService.IsConversationAllowed(p.talkCountToday, (int)FavorabilityConstant.ConversationDailyCap))
            {
                AWorkerTask.LogProvider($"[Favor] {worker.name} 今日对话好感已达上限，不再增加", LogManager.LogLevelEnum.Debug);
                return;
            }

            p.talkCountToday++;
            this.ModifyWithPlayer(worker, FavorabilityConstant.ConversationDelta, "对话");

            // 心智层：对话结束事件记忆（对玩家，事件点单次调用）
            if (Core.ServiceLocator.TryGet<WorkerMindService>(out WorkerMindService mindService))
            {
                mindService.RecordEvent(worker, WorkerMindConstant.EVT_CONVERSATION,
                    MemoryValence.Positive, WorkerMindService.PlayerTargetName, 20f, "和玩家聊了会儿天");
            }
        }

        // ---- 门控谓词 ----

        /// <summary>是否愿意接受玩家悬赏（对玩家好感 &gt;= 阈值）。</summary>
        public bool IsWillingForPlayerBounty(AWorker worker)
        {
            return FavorabilityRuleService.IsWillingForPlayerBounty(this.GetFavorabilityWithPlayer(worker));
        }

        /// <summary>是否愿意接受指定 Worker 发布的悬赏（对该 Worker 好感 &gt;= 阈值）。</summary>
        public bool IsWillingForWorkerBounty(AWorker worker, int issuerId)
        {
            return FavorabilityRuleService.IsWillingForWorkerBounty(this.GetFavorability(worker, issuerId));
        }

        // ---- 死亡清理 ----

        /// <summary>Worker 死亡时清理其好感档案与所有引用（DeathProvider 调用）。deadName 非空时同步清理各 Worker 心智自发关系。</summary>
        public void RemoveDeadWorker(int instanceId, string deadName = null)
        {
            this.profiles.Remove(instanceId);

            foreach (KeyValuePair<int, FavorabilityProfile> kv in this.profiles)
            {
                kv.Value.toWorkers?.Remove(instanceId);
            }

            var keysToRemove = new List<long>();
            foreach (KeyValuePair<long, float> kv in this.proximityGains)
            {
                int from = (int)(kv.Key >> 32);
                int to = (int)(uint)kv.Key;
                if (from == instanceId || to == instanceId) keysToRemove.Add(kv.Key);
            }
            foreach (long key in keysToRemove) this.proximityGains.Remove(key);
            this.playerHelpCooldowns.Remove(instanceId);

            // 心智层：清理各 Worker 对死者的自发关系（关系键为 name，跨存档稳定引用）
            if (!string.IsNullOrEmpty(deadName))
            {
                WorkerManager wm = Core.ServiceLocator.Get<WorkerManager>();
                if (wm?.Characters != null)
                {
                    foreach (AWorker w in wm.Characters)
                    {
                        if (w == null || w.GetInstanceID() == instanceId)
                        {
                            continue;
                        }

                        AWorker.WorkerData wd = w.CharacterDataLAB as AWorker.WorkerData;
                        if (wd == null)
                        {
                            continue;
                        }

                        WorkerMindData.Ensure(wd);
                        if (WorkerRelationshipRuleService.Remove(wd.Mind, deadName))
                        {
                            AWorkerTask.LogProvider(
                                $"[MindDiag] {w.name} 清理了对 {deadName} 的关系",
                                LogManager.LogLevelEnum.Debug);
                        }
                    }
                }
            }

            AWorkerTask.LogProvider($"[Favor] 清理死亡 Worker[{instanceId}] 的好感数据", LogManager.LogLevelEnum.Debug);
        }

        // ---- Tick：接近/共事扫描 ----

        public void Tick(float deltaTime)
        {
            this.proximityTimer += deltaTime;
            if (this.proximityTimer < FavorabilityConstant.ProximityTickInterval) return;
            this.proximityTimer = 0f;
            this.ScanProximity();
        }

        /// <summary>
        /// 节流扫描：距离 &lt; 半径的 Worker 对互相缓慢累积好感；与 Player 相邻的 Worker 也累积。
        /// 每对累计上限 ProximityMaxPerPair，防自动刷满。
        /// </summary>
        private void ScanProximity()
        {
            WorkerManager wm = Core.ServiceLocator.Get<WorkerManager>();
            if (wm == null) return;

            List<AWorker> workers = wm.Characters;
            int n = workers.Count;
            if (n == 0) return;

            float radiusSq = FavorabilityConstant.ProximityRadiusMapTiles * FavorabilityConstant.ProximityRadiusMapTiles;
            Player player = Core.ServiceLocator.Get<PlayerManager>()?.Mine;

            for (int i = 0; i < n; i++)
            {
                AWorker a = workers[i];
                if (a == null) continue;
                Vector3 pa = a.transform.position;

                if (player != null)
                {
                    if ((pa - player.transform.position).sqrMagnitude <= radiusSq)
                    {
                        this.AddProximityGain(a, PlayerId, FavorabilityConstant.ProximityPerTickWithPlayer);
                    }
                }

                for (int j = i + 1; j < n; j++)
                {
                    AWorker b = workers[j];
                    if (b == null) continue;
                    if ((pa - b.transform.position).sqrMagnitude <= radiusSq)
                    {
                        int aId = a.GetInstanceID();
                        int bId = b.GetInstanceID();
                        this.AddProximityGain(a, bId, FavorabilityConstant.ProximityPerTick);
                        this.AddProximityGain(b, aId, FavorabilityConstant.ProximityPerTick);
                    }
                }
            }
        }

        private void AddProximityGain(AWorker from, int toId, float perTick)
        {
            long key = ((long)from.GetInstanceID() << 32) | (uint)toId;
            float acc = this.proximityGains.TryGetValue(key, out float v) ? v : 0f;
            if (acc >= FavorabilityConstant.ProximityMaxPerPair) return;

            float next = Mathf.Min(acc + perTick, FavorabilityConstant.ProximityMaxPerPair);
            this.proximityGains[key] = next;
            this.ModifyFavorability(from, toId, next - acc, "接近/共事");
        }

        // ---- 存档 ----

        [Serializable]
        public class FavorabilitySaveData
        {
            public List<FavorabilityEntry> Entries = new List<FavorabilityEntry>();
        }

        [Serializable]
        public class FavorabilityEntry
        {
            public string HolderName;
            public string TargetName; // "PLAYER" 表示对玩家，否则为对方 Worker 名字
            public float Value;
        }

        /// <inheritdoc/>
        public override void SaveData()
        {
            base.SaveData();
            FavorabilitySaveData data = new FavorabilitySaveData();
            WorkerManager wm = Core.ServiceLocator.Get<WorkerManager>();
            if (wm == null) return;

            foreach (KeyValuePair<int, FavorabilityProfile> kv in this.profiles)
            {
                AWorker holder = this.FindWorker(kv.Key, wm);
                if (holder == null) continue;
                string holderName = holder.name;

                data.Entries.Add(new FavorabilityEntry
                {
                    HolderName = holderName,
                    TargetName = PlayerTargetName,
                    Value = kv.Value.toPlayer,
                });

                if (kv.Value.toWorkers != null)
                {
                    foreach (KeyValuePair<int, float> wk in kv.Value.toWorkers)
                    {
                        AWorker target = this.FindWorker(wk.Key, wm);
                        if (target == null) continue;
                        data.Entries.Add(new FavorabilityEntry
                        {
                            HolderName = holderName,
                            TargetName = target.name,
                            Value = wk.Value,
                        });
                    }
                }
            }

            DataTool.SaveDataByBinary(GlobalData.ConfigFile.GetPath(this.GetType().Name), data);
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            base.LoadData();
            FavorabilitySaveData data = DataTool.LoadDataByBinary<FavorabilitySaveData>(GlobalData.ConfigFile.GetPath(this.GetType().Name));
            if (data == null) return;

            this.profiles.Clear();

            WorkerManager wm = Core.ServiceLocator.Get<WorkerManager>();
            if (wm == null) return;

            // name → instanceID 映射（读档时 Worker 已重建）
            var nameToId = new Dictionary<string, int>();
            foreach (AWorker w in wm.Characters)
            {
                if (w != null) nameToId[w.name] = w.GetInstanceID();
            }

            foreach (FavorabilityEntry e in data.Entries)
            {
                if (e == null || string.IsNullOrEmpty(e.HolderName)) continue;
                if (!nameToId.TryGetValue(e.HolderName, out int holderId)) continue; // 匹配不到丢弃（死亡/消失）

                FavorabilityProfile p = this.GetOrCreateProfile(holderId);
                if (e.TargetName == PlayerTargetName)
                {
                    p.toPlayer = FavorabilityRuleService.Clamp(e.Value);
                }
                else
                {
                    if (!nameToId.TryGetValue(e.TargetName, out int targetId)) continue;
                    p.toWorkers ??= new Dictionary<int, float>();
                    p.toWorkers[targetId] = FavorabilityRuleService.Clamp(e.Value);
                }
            }

            AWorkerTask.LogProvider($"[Favor] 读档好感度：{this.profiles.Count} 个 Worker", LogManager.LogLevelEnum.Debug);
        }

        // ---- helper ----

        private FavorabilityProfile GetOrCreateProfile(int id)
        {
            if (!this.profiles.TryGetValue(id, out FavorabilityProfile p))
            {
                p = new FavorabilityProfile();
                this.profiles[id] = p;
            }
            return p;
        }

        private AWorker FindWorker(int instanceId, WorkerManager wm)
        {
            foreach (AWorker w in wm.Characters)
            {
                if (w != null && w.GetInstanceID() == instanceId) return w;
            }
            return null;
        }

        /// <summary>当前游戏日索引（对话每日限额按游戏日重置）。</summary>
        private int GetGameDayIndex()
        {
            IGameTime gt = Core.ServiceLocator.Get<IGameTime>();
            if (gt == null) return 0;
            return (int)(gt.Time / FavorabilityConstant.GameDaySeconds);
        }

        /// <summary>好感变化联动 Mood：|delta|&gt;=5 时 Mood 微调 ±delta*0.05（clamp ±5）。</summary>
        private void ApplyMoodDelta(AWorker worker, float delta)
        {
            AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;
            if (wd == null) return;
            float moodDelta = FavorabilityRuleService.GetMoodDelta(delta);
            if (moodDelta == 0f) return;
            wd.Personality = wd.Personality.AfterFavorabilityChange(moodDelta);
        }
    }
}
