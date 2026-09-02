namespace LAB2D.Gameplay
{
    using System.Collections.Generic;
    using LAB2D;
    using LAB2D.Character.Worker.Task.Individual;
    using LAB2D.Domain.Character.Growth;
    using LAB2D.Domain.Time;
    using LAB2D.Domain.Worker;
    using LAB2D.Enum;
    using LAB2D.Item.Build;
    using UnityEngine;

    /// <summary>
    /// 防守夜调度（M2A 包 2.1）— 入夜时按 <see cref="DefenceDraftRuleService"/>
    /// 给每名 Worker 决策并派发 <see cref="WorkerDefendTask"/>：参战者驻守山门核心旁、
    /// 躲避者缩回床/家旁、趁乱者溜到远处游荡（觉醒者优先参战，由打分保证）。
    /// 任务时长 = 距黎明秒数，到点自然 Finish，无需结束清理；
    /// 单例，由 GlobalInit 注册（IInitializable 订阅相位事件，无逐帧逻辑）。
    /// </summary>
    public class WorkerDefenceManager : Singleton<WorkerDefenceManager>, IInitializable
    {
        /// <summary>趁乱者游荡点在其当前位置周围的搜索半径（格）。</summary>
        private const int LootPosSearchRadius = 6;

        /// <summary>躲床/参战/趁乱的派发气泡（每人入夜弹一句）。</summary>
        private static readonly string[] FightBubbles = new[]
        {
            "妖兽来了，守住山门！",
            "该我上阵了！",
            "别想踏进小镇一步！",
        };
        private static readonly string[] ShelterBubbles = new[]
        {
            "呜……太可怕了，我要躲好。",
            "打仗的事交给别人吧……",
            "瑟瑟发抖，千万别找到我……",
        };
        private static readonly string[] LootBubbles = new[]
        {
            "乱起来了……正好溜边看看。",
            "他们打他们的，我走我的。",
            "趁着夜里没人注意……",
        };

        private bool isInitialized;

        /// <summary>上次draft的游戏日（防同夜重派：读档/边界情况下的兜底）。</summary>
        private int lastDraftDay = -1;

        /// <inheritdoc/>
        public void Initialize()
        {
            if (this.isInitialized)
            {
                return;
            }

            this.isInitialized = true;
            EventBus.Instance.Subscribe<GamePhaseChangedEvent>(this.OnPhaseChanged);
            AWorkerTask.LogProvider("[DefenceDiag] WorkerDefenceManager 初始化完成", LogManager.LogLevelEnum.Debug);
        }

        /// <summary>相位事件：入夜即draft（与 WaveManager.OnPhaseChanged 同模式）。</summary>
        private void OnPhaseChanged(GamePhaseChangedEvent e)
        {
            if (e.NewPhase != GamePhase.Night)
            {
                return;
            }

            this.RunNightDraft();
        }

        /// <summary>入夜点名：逐 Worker 决策 + 派防守待命任务（事件点，每夜一次）。</summary>
        private void RunNightDraft()
        {
            WorkerManager wm = Core.ServiceLocator.Get<WorkerManager>();
            if (wm == null || wm.Characters == null || wm.Characters.Count == 0)
            {
                return;
            }

            // 无山门核心（未放置）：无处可守，不部署（村民按各自日常过夜）
            if (!Core.ServiceLocator.TryGet<MountainGateManager>(out MountainGateManager gate)
                || !gate.IsCorePlaced)
            {
                AWorkerTask.LogProvider(
                    "[DefenceDiag] 入夜但山门核心未放置，跳过防守draft",
                    LogManager.LogLevelEnum.Debug);
                return;
            }

            int day = this.GetGameDayIndex();
            if (day == this.lastDraftDay)
            {
                return;
            }

            this.lastDraftDay = day;

            // 驻守时长 = 距黎明秒数（任务到点自然 Finish，收工回日常）
            float nightSeconds = DayNightRuleService.SecondsUntilPhaseStart(
                GameTimeManager.Instance.CurGameTime,
                GlobalData.GameDayTime,
                GamePhase.Dawn);

            List<Vector3Int> coreCells = ABuildItem.GetOccupiedPositions(
                gate.CorePosition, MountainGateManager.CoreSize, MountainGateManager.CoreSize,
                AWorkerTask.RectType.BottomLeft);
            List<Vector3Int> defendPositions = this.CollectDefendPositions(coreCells);
            int defendPosCursor = 0;
            FavorabilityManager favorability = Core.ServiceLocator.Get<FavorabilityManager>();

            foreach (AWorker worker in wm.Characters)
            {
                if (worker == null || worker.CharacterDataLAB == null)
                {
                    continue;
                }

                AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;
                if (wd == null)
                {
                    continue;
                }

                GrowthData.Ensure(ref wd.Growth);
                DefenceDraftInput input = new DefenceDraftInput
                {
                    Mood = wd.Personality.Mood,
                    Ambition = wd.Personality.Ambition,
                    Diligence = wd.Personality.Diligence,
                    Sociality = wd.Personality.Sociality,
                    Greed = wd.Greed,
                    Stress = wd.CurStress,
                    Morale = wd.CurMorale,
                    FavorWithPlayer = favorability != null
                        ? favorability.GetFavorabilityWithPlayer(worker)
                        : FavorabilityRuleService.InitialFavorability,
                    HasAwakenedPower = wd.Growth.AwakenedPowerIds != null
                        && wd.Growth.AwakenedPowerIds.Count > 0,
                    RealmIndex = wd.Growth.RealmIndex,
                };

                DefenceResponse response = DefenceDraftRuleService.Decide(in input);
                Vector3Int target = response switch
                {
                    DefenceResponse.Fight => this.NextDefendPosition(defendPositions, ref defendPosCursor, coreCells),
                    DefenceResponse.ShelterInBed => this.GetShelterPosition(worker, wd),
                    _ => this.FindLootPosition(worker),
                };

                WorkerDefendTask task = new WorkerDefendTask.DefendTaskBuilder()
                    .SetTarget(target)
                    .SetWorker(worker)
                    .SetDuration(nightSeconds)
                    .Build();
                worker.SetTask(task, WorkerTaskSource.PushAssignment);
                worker.ShowMindBubble(this.PickBubble(response));

                AWorkerTask.LogProvider(
                    $"[DefenceDiag] {worker.name} 防守响应={response} 目标=({target.x},{target.y}) " +
                    $"觉醒={input.HasAwakenedPower} 境界={input.RealmIndex} 贪婪={input.Greed:F0} 压力={input.Stress:F0} 士气={input.Morale:F0}",
                    LogManager.LogLevelEnum.Debug);
            }
        }

        /// <summary>
        /// 收集核心周边可站立的待命位：占用格的 4 正交邻居去重、
        /// 剔除核心自身占用格、按通行性过滤。不足时派发端循环复用（同格可站多人）。
        /// </summary>
        private List<Vector3Int> CollectDefendPositions(List<Vector3Int> coreCells)
        {
            HashSet<Vector3Int> coreSet = new HashSet<Vector3Int>(coreCells);
            List<Vector3Int> result = new List<Vector3Int>();
            HashSet<Vector3Int> seen = new HashSet<Vector3Int>();
            Vector3Int[] offsets = new[]
            {
                new Vector3Int(0, 1, 0), new Vector3Int(1, 0, 0),
                new Vector3Int(0, -1, 0), new Vector3Int(-1, 0, 0),
            };

            foreach (Vector3Int cell in coreCells)
            {
                foreach (Vector3Int offset in offsets)
                {
                    Vector3Int pos = cell + offset;
                    if (coreSet.Contains(pos) || !seen.Add(pos))
                    {
                        continue;
                    }

                    if (AWorkerTask.WalkabilityProvider(pos.x, pos.y))
                    {
                        result.Add(pos);
                    }
                }
            }

            return result;
        }

        /// <summary>取下一个参战待命位（用尽后从头循环复用）。</summary>
        private Vector3Int NextDefendPosition(List<Vector3Int> defendPositions, ref int cursor, List<Vector3Int> coreCells)
        {
            if (defendPositions.Count == 0)
            {
                // 核心被围死：退化为第一个占用格旁（寻路会自然停在最近可达处）
                return coreCells[0];
            }

            Vector3Int pos = defendPositions[cursor % defendPositions.Count];
            cursor++;
            return pos;
        }

        /// <summary>躲避位：有家回床边，无家原地缩着。</summary>
        private Vector3Int GetShelterPosition(AWorker worker, AWorker.WorkerData wd)
        {
            if (wd.HomePosition != null)
            {
                return Vector3IntLAB.ToVector3Int(wd.HomePosition);
            }

            return AWorkerTask.TileMapWorldToMapProvider(worker.transform.position);
        }

        /// <summary>趁乱游荡位：在其当前位置周围随机找一块可通行格（远离战场溜边）。</summary>
        private Vector3Int FindLootPosition(AWorker worker)
        {
            Vector3Int origin = AWorkerTask.TileMapWorldToMapProvider(worker.transform.position);
            for (int attempt = 0; attempt < 12; attempt++)
            {
                Vector3Int candidate = origin + new Vector3Int(
                    Random.Range(-LootPosSearchRadius, LootPosSearchRadius + 1),
                    Random.Range(-LootPosSearchRadius, LootPosSearchRadius + 1), 0);
                if (AWorkerTask.WalkabilityProvider(candidate.x, candidate.y))
                {
                    return candidate;
                }
            }

            return origin; // 周围全不可通行：原地徘徊
        }

        private string PickBubble(DefenceResponse response)
        {
            return response switch
            {
                DefenceResponse.Fight => FightBubbles[Random.Range(0, FightBubbles.Length)],
                DefenceResponse.ShelterInBed => ShelterBubbles[Random.Range(0, ShelterBubbles.Length)],
                _ => LootBubbles[Random.Range(0, LootBubbles.Length)],
            };
        }

        private int GetGameDayIndex()
        {
            IGameTime gt = Core.ServiceLocator.Get<IGameTime>();
            return gt == null ? 0 : (int)(gt.Time / FavorabilityConstant.GameDaySeconds);
        }
    }
}
