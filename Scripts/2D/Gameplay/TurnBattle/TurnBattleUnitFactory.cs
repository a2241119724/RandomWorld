namespace LAB2D.Gameplay.TurnBattle
{
    using System.Collections.Generic;
    using LAB2D.Character;
    using LAB2D.Character.Enemy;
    using LAB2D.Character.Player;
    using LAB2D.Character.Worker;
    using LAB2D.Domain.Character;
    using LAB2D.Domain.Gameplay.Cultivation;
    using LAB2D.Domain.TurnBattle;
    using SkillType = LAB2D.Enum.SkillType;

    /// <summary>
    /// 参战单位快照工厂 — 大世界 Character → <see cref="TurnBattleUnit"/>。
    /// 战斗内只读写快照，结束由 ResultWriter 一次性写回，
    /// 避免冻结大世界时触发 ReduceHp 副作用链（状态切换/仇恨/粒子卡住）。
    /// </summary>
    public static class TurnBattleUnitFactory
    {
        /// <summary>玩家快照：含 8 槽技能（过滤 Movement/Pull — 冲刺/念力无回合制语义）。</summary>
        public static TurnBattleUnit CreateFromPlayer(Player player)
        {
            TurnBattleUnit unit = CreateCommon(player, TurnBattleFaction.Ally, isPlayer: true);

            if (ServiceLocator.TryGet(out SkillManager sm) && sm.Skills != null)
            {
                MapSkills(unit, sm.Skills);
            }

            return unit;
        }

        /// <summary>Worker 队友快照：第一版只有普攻（技能管线已就绪，后续可配）。</summary>
        public static TurnBattleUnit CreateFromWorker(AWorker worker)
        {
            return CreateCommon(worker, TurnBattleFaction.Ally, isPlayer: false);
        }

        /// <summary>Enemy 快照：无灵根恒中性；附带状态演示技能（毒牙/撞击，调平衡时关开关）。</summary>
        public static TurnBattleUnit CreateFromEnemy(AEnemy enemy)
        {
            TurnBattleUnit unit = CreateCommon(enemy, TurnBattleFaction.Enemy, isPlayer: false);
            unit.LingGenElements.Clear();

            if (EnemyStatusSkillsEnabled)
            {
                AddEnemyStatusSkills(unit);
            }

            return unit;
        }

        /// <summary>敌方状态演示技能开关（演示位：让中毒/眩晕在实战可见，调平衡时置 false 恢复纯普攻）。</summary>
        private const bool EnemyStatusSkillsEnabled = true;

        /// <summary>
        /// 敌方演示技能（直接构造快照，不依赖 SkillManager）：
        /// 毒牙 — 倍率同普攻、35% 中毒 3 回合（每回合 6% MaxHp）；
        /// 撞击 — 0.8 倍率冷却 3 回合、20% 眩晕 1 回合。
        /// AI 按期望伤害评分自然选用。
        /// </summary>
        private static void AddEnemyStatusSkills(TurnBattleUnit unit)
        {
            unit.Skills.Add(new TurnBattleSkillState
            {
                SkillId = "enemy_poison_fang",
                SkillName = "毒牙",
                Type = SkillType.SingleTarget,
                EffectMultiplier = 1f,
                CooldownTurns = 2,
                StatusKind = TurnBattleStatusKind.Poison,
                StatusChance = 0.35f,
                StatusDurationTurns = 3,
                StatusValueRatio = 0.06f,
            });

            unit.Skills.Add(new TurnBattleSkillState
            {
                SkillId = "enemy_stun_ram",
                SkillName = "撞击",
                Type = SkillType.SingleTarget,
                EffectMultiplier = 0.8f,
                CooldownTurns = 3,
                StatusKind = TurnBattleStatusKind.Stun,
                StatusChance = 0.2f,
                StatusDurationTurns = 1,
            });
        }

        private static TurnBattleUnit CreateCommon(Character character, TurnBattleFaction faction, bool isPlayer)
        {
            Character.CharacterData data = character.CharacterDataLAB;
            return new TurnBattleUnit
            {
                UnitId = data.Id,
                DisplayName = ResolveDisplayName(character, data),
                Faction = faction,
                Hp = data.Hp,
                MaxHp = data.MaxHp,
                Mp = data.Mp,
                MaxMp = data.MaxMp,
                Level = data.Level,
                Stats = new BattleStats(data.ATN, data.INT, data.DEF, data.RES, data.CRT, data.CSD, data.SPD, data.HIT),
                LingGenElements = ReadLingGen(data),
                IsPlayerUnit = isPlayer,
            };
        }

        private static string ResolveDisplayName(Character character, Character.CharacterData data)
        {
            return !string.IsNullOrEmpty(data.Name) ? data.Name : character.name;
        }

        /// <summary>灵根快照：Growth.LingGenElements 存的是 Element 枚举 int 值。</summary>
        private static List<Element> ReadLingGen(Character.CharacterData data)
        {
            List<Element> result = new List<Element>();
            if (data.Growth?.LingGenElements == null)
            {
                return result;
            }

            foreach (int raw in data.Growth.LingGenElements)
            {
                if (System.Enum.IsDefined(typeof(Element), raw))
                {
                    result.Add((Element)raw);
                }
            }

            return result;
        }

        /// <summary>技能快照映射：实时秒冷却/状态时长 → 回合数（≤3s 归 0）；元素与状态字段直拷。</summary>
        private static void MapSkills(TurnBattleUnit unit, List<SkillData> skills)
        {
            foreach (SkillData skill in skills)
            {
                if (skill == null || skill.SkillType == SkillType.Movement || skill.SkillType == SkillType.Pull)
                {
                    continue;
                }

                unit.Skills.Add(new TurnBattleSkillState
                {
                    SkillId = skill.SkillId,
                    SkillName = skill.SkillName,
                    ManaCost = skill.ManaCost,
                    Type = skill.SkillType,
                    EffectType = skill.EffectType,
                    EffectMultiplier = skill.EffectMultiplier,
                    ScaleByInt = skill.ScaleByInt,
                    Level = skill.Level,
                    CooldownTurns = TurnBattleRuleService.ConvertSecondsToTurns(skill.BaseCooldown),
                    BuffDurationTurns = TurnBattleRuleService.ConvertSecondsToTurns(skill.BuffDuration),
                    Element = skill.Element,
                    StatusKind = skill.StatusKind,
                    StatusChance = skill.StatusChance,
                    StatusDurationTurns = TurnBattleRuleService.ConvertSecondsToTurns(skill.StatusDuration),
                    StatusValueRatio = skill.StatusValueRatio,
                });
            }
        }
    }
}
