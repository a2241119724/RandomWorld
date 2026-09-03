namespace LAB2D.Gameplay.TurnBattle
{
    using System;
    using System.Collections.Generic;
    using LAB2D.Character;
    using LAB2D.Character.Enemy;
    using LAB2D.Character.Player;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.TurnBattle;
    using UnityEngine;

    /// <summary>
    /// 回合制战斗编排器 — 快照构建 → 面板驱动 → 回合结算 → 结果写回。
    /// 回合由 UI 事件驱动（非 ITickable）：玩家提交行动后整回合结算，
    /// Resolution 列表交 UI 演出（unscaledDeltaTime），演完回调进入下一回合。
    /// </summary>
    public sealed class TurnBattleManager : Singleton<TurnBattleManager>
    {
        /// <summary>联机检测 — 复用 GameServices 既有缝（timeScale 本地性使联机快照不安全，第一版硬边界）。</summary>
        public static bool IsNetworkOnline => GameServices.NetworkIsOnlineProvider();

        /// <summary>战斗进行中（Player.Update 输入拦截与检测器跳过依赖此标志）。</summary>
        public bool IsActive { get; private set; }

        /// <summary>当前战斗状态快照（未开战为 null）。</summary>
        public TurnBattleState State { get; private set; }

        /// <summary>大世界交战检测器（提示 HUD 与 G 键加入共用）。</summary>
        public BattleEncounterDetector Detector { get; } = new BattleEncounterDetector();

        /// <summary>取单位大世界立绘（卡片头像用；快照层不持有 Unity 资源）。</summary>
        public UnityEngine.Sprite GetPortrait(long unitId)
        {
            return this.worldUnits.TryGetValue(unitId, out Character character)
                ? character.GetComponent<SpriteRenderer>()?.sprite
                : null;
        }

        /// <summary>快照 UnitId → 大世界 Character 映射（写回用）。</summary>
        private readonly Dictionary<long, Character> worldUnits = new Dictionary<long, Character>();

        /// <summary>判负时击倒玩家的敌人（死亡惩罚归因，Defeat 路径写回用）。</summary>
        private AEnemy defeatKiller;

        /// <summary>战斗开始 — UI 打开面板、绑定卡片。</summary>
        public event Action<TurnBattleState> BattleStarted;

        /// <summary>一整回合结算完成 — UI 按序演出 Resolution 列表，演完调 <see cref="NotifyPresentationFinished"/>。</summary>
        public event Action<IReadOnlyList<TurnBattleActionResolution>> TurnResolved;

        /// <summary>等待玩家行动 — UI 启用行动菜单。</summary>
        public event Action<TurnBattleState> AwaitingPlayerAction;

        /// <summary>战斗结束且写回完成 — UI 关闭面板（此时才可安全 Close 恢复 timeScale）。</summary>
        public event Action<TurnBattleResult> BattleFinished;

        /// <summary>
        /// 尝试开战。校验：非战斗中 / 非联机 / 双方有存活 / 玩家存活。
        /// 成功后注入随机源、构建快照、发 Started 事件（面板由 UI 层在事件中打开）。
        /// </summary>
        public bool TryStartBattle(BattleEncounter encounter, Player player, out string failReason)
        {
            failReason = string.Empty;
            if (this.IsActive)
            {
                failReason = "已在战斗中";
                return false;
            }

            if (IsNetworkOnline)
            {
                failReason = "联机模式暂不支持回合制战斗";
                return false;
            }

            if (player == null || player.CharacterDataLAB == null || player.CharacterDataLAB.Hp <= 0f)
            {
                failReason = "玩家不可参战";
                return false;
            }

            if (encounter == null || !encounter.HasAliveOnBothSides)
            {
                failReason = "交战双方已有一方倒下";
                return false;
            }

            // 随机源注入（战斗全程保持；Domain 测试未注入时为中性 0.5）
            TurnBattleRuleService.RandomFloatProvider = (min, max) => UnityEngine.Random.Range(min, max);

            TurnBattleState state = new TurnBattleState();
            this.worldUnits.Clear();
            this.defeatKiller = null;

            TurnBattleUnit playerUnit = TurnBattleUnitFactory.CreateFromPlayer(player);
            state.Units.Add(playerUnit);
            this.worldUnits[playerUnit.UnitId] = player;

            foreach (AWorker worker in encounter.Workers)
            {
                if (worker == null || worker.CharacterDataLAB == null || worker.CharacterDataLAB.Hp <= 0f)
                {
                    continue;
                }

                TurnBattleUnit unit = TurnBattleUnitFactory.CreateFromWorker(worker);
                if (this.worldUnits.ContainsKey(unit.UnitId))
                {
                    continue;
                }

                state.Units.Add(unit);
                this.worldUnits[unit.UnitId] = worker;
            }

            foreach (AEnemy enemy in encounter.Enemies)
            {
                if (enemy == null || enemy.CharacterDataLAB == null || enemy.CharacterDataLAB.Hp <= 0f)
                {
                    continue;
                }

                TurnBattleUnit unit = TurnBattleUnitFactory.CreateFromEnemy(enemy);
                if (this.worldUnits.ContainsKey(unit.UnitId))
                {
                    continue;
                }

                state.Units.Add(unit);
                this.worldUnits[unit.UnitId] = enemy;
            }

            this.State = state;
            this.IsActive = true;
            state.Phase = TurnBattlePhase.AwaitPlayerAction;

            AWorkerTask.LogProvider(
                $"[BattleDiag] 回合制战斗开始：我方 {state.AliveAllies.Count} 人（含玩家）vs 敌方 {state.AliveEnemies.Count} 人，第 1 回合",
                LogManager.LogLevelEnum.Debug);

            ServiceLocator.TryGet(out EventBus eventBus);
            eventBus?.Publish(new TurnBattleStartedEvent { UnitIds = new List<long>(this.worldUnits.Keys) });

            this.BattleStarted?.Invoke(state);
            this.AwaitingPlayerAction?.Invoke(state);
            return true;
        }

        /// <summary>
        /// 玩家提交本回合行动 — SPD 行动序整回合结算（敌方/队友走 AI），
        /// 结算列表发 TurnResolved 交 UI 演出。
        /// </summary>
        public void SubmitPlayerAction(TurnBattleAction action)
        {
            if (!this.IsActive || this.State == null || this.State.Phase != TurnBattlePhase.AwaitPlayerAction)
            {
                return;
            }

            TurnBattleState state = this.State;
            state.Phase = TurnBattlePhase.Resolving;

            List<TurnBattleActionResolution> resolutions = new List<TurnBattleActionResolution>();
            List<TurnBattleUnit> order = TurnBattleRuleService.DetermineTurnOrder(state);
            foreach (TurnBattleUnit unit in order)
            {
                if (state.Phase == TurnBattlePhase.BattleOver)
                {
                    break;
                }

                if (!unit.CanAct)
                {
                    continue; // 本回合更早时刻被中途打倒
                }

                TurnBattleAction unitAction = unit.IsPlayerUnit
                    ? action
                    : SelectAutoAction(state, unit);
                TurnBattleActionResolution resolution = TurnBattleRuleService.ResolveAction(state, unit, unitAction);
                resolutions.Add(resolution);
                this.TrackDefeatKiller(resolution, state);

                TurnBattleResult? result = TurnBattleRuleService.EvaluateBattleResult(state);
                if (result != null)
                {
                    state.Phase = TurnBattlePhase.BattleOver;
                    state.Result = result.Value;
                }
            }

            if (state.Phase != TurnBattlePhase.BattleOver)
            {
                TurnBattleRuleService.AdvanceRound(state);
            }

            AWorkerTask.LogProvider(
                $"[BattleDiag] 第 {state.Round} 回合结算：{resolutions.Count} 次行动，"
                + $"我方存活 {state.AliveAllies.Count}、敌方存活 {state.AliveEnemies.Count}"
                + (state.Phase == TurnBattlePhase.BattleOver ? $"，战斗结束（{state.Result}）" : string.Empty),
                LogManager.LogLevelEnum.Debug);

            this.TurnResolved?.Invoke(resolutions);
        }

        /// <summary>
        /// UI 演出完毕回调 — 战斗未结束则回到等待玩家行动；
        /// 已结束则写回大世界、发 Finished 事件（UI 此时 Close 面板恢复 timeScale）。
        /// </summary>
        public void NotifyPresentationFinished()
        {
            if (!this.IsActive || this.State == null)
            {
                return;
            }

            if (this.State.Phase == TurnBattlePhase.BattleOver)
            {
                this.FinishBattle();
                return;
            }

            this.State.Phase = TurnBattlePhase.AwaitPlayerAction;
            this.AwaitingPlayerAction?.Invoke(this.State);
        }

        /// <summary>
        /// 中止战斗不作写回（面板被强制关闭等异常路径）— 快照作废，大世界保持战前状态。
        /// 正常结束走 <see cref="NotifyPresentationFinished"/> 的写回路径。
        /// </summary>
        public void AbortBattle()
        {
            if (!this.IsActive)
            {
                return;
            }

            AWorkerTask.LogProvider("[BattleDiag] 战斗中止（快照作废不写回）", LogManager.LogLevelEnum.Warning);
            this.ResetBattleState();
        }

        private void FinishBattle()
        {
            TurnBattleState state = this.State;
            Player player = null;
            if (this.worldUnits.TryGetValue(state.PlayerUnit?.UnitId ?? -1, out Character playerCharacter))
            {
                player = playerCharacter as Player;
            }

            // 写回发生在面板关闭前：Enemy 死亡经 ReduceHp 走标准管线，
            // 延迟掉落在面板 Close、timeScale 恢复后自然执行
            TurnBattleResultWriter.WriteBack(state, this.worldUnits, player, this.defeatKiller);

            List<long> defeatedIds = new List<long>();
            foreach (TurnBattleUnit unit in state.Units)
            {
                if (unit.Faction == TurnBattleFaction.Enemy && unit.IsDown)
                {
                    defeatedIds.Add(unit.UnitId);
                }
            }

            ServiceLocator.TryGet(out EventBus eventBus);
            eventBus?.Publish(new TurnBattleEndedEvent
            {
                Result = state.Result,
                Round = state.Round,
                DefeatedEnemyUnitIds = defeatedIds,
            });

            AWorkerTask.LogProvider(
                $"[BattleDiag] 战斗结束：{state.Result}，共 {state.Round} 回合，击败敌人 {defeatedIds.Count} 个",
                LogManager.LogLevelEnum.Debug);

            TurnBattleResult result = state.Result;
            this.ResetBattleState();
            this.BattleFinished?.Invoke(result);
        }

        private void ResetBattleState()
        {
            this.State = null;
            this.IsActive = false;
            this.worldUnits.Clear();
            this.defeatKiller = null;
            TurnBattleRuleService.RandomFloatProvider = null;
        }

        /// <summary>
        /// 非玩家单位行动选择：Enemy 走 Domain AI（含技能评分与低血偏置）；
        /// Worker 队友第一版只有普攻 — 取敌方首个存活单位（AI 深度留后续）。
        /// </summary>
        private static TurnBattleAction SelectAutoAction(TurnBattleState state, TurnBattleUnit unit)
        {
            if (unit.Faction == TurnBattleFaction.Enemy)
            {
                return TurnBattleRuleService.SelectEnemyAction(state, unit);
            }

            List<TurnBattleUnit> opponents = state.AliveEnemies;
            TurnBattleAction action = new TurnBattleAction { Kind = TurnBattleActionKind.Attack };
            if (opponents.Count > 0)
            {
                action.TargetUnitId = opponents[0].UnitId;
            }

            return action;
        }

        /// <summary>记录击倒玩家的敌人（Defeat 写回时归因死亡惩罚）。</summary>
        private void TrackDefeatKiller(TurnBattleActionResolution resolution, TurnBattleState state)
        {
            TurnBattleUnit player = state.PlayerUnit;
            if (player == null || !player.IsDown)
            {
                return;
            }

            foreach (TurnBattleEffectEntry effect in resolution.Effects)
            {
                if (effect.Kind == TurnBattleEffectKind.Down
                    && effect.TargetId == player.UnitId
                    && this.worldUnits.TryGetValue(effect.ActorId, out Character actor)
                    && attackerIsEnemy(actor))
                {
                    this.defeatKiller = actor as AEnemy;
                    return;
                }
            }
        }

        private static bool attackerIsEnemy(Character character)
        {
            return character is AEnemy;
        }
    }
}
