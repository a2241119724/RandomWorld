namespace LAB2D.Gameplay
{
    using LAB2D;
    using LAB2D.Character.Player;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Player;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using GameCharacter = LAB2D.Character.Character;

    /// <summary>
    /// 主动技能系统管理器。
    /// 负责技能生命周期管理：冷却追踪、法力校验、技能激活、等级升级、Buff 计时。
    /// 通过 Singleton 模式全局访问，在 GlobalInit.Start 中初始化。
    /// 不涉及存档持久化和 Photon 同步。
    /// </summary>
    public class SkillManager : Singleton<SkillManager>, ISkillManager, IInitializable
    {
        /// <summary>已注册的技能列表（按 SlotIndex 排序）</summary>
        public List<SkillData> Skills { get; private set; }

        /// <summary>是否已完成初始化</summary>
        public bool IsInitialized { get; private set; }

        /// <summary>玩家可用升级点数（通过累积经验获得）</summary>
        public int AvailableUpgradePoints { get; private set; }

        /// <summary>
        /// 技能激活事件：参数为激活的技能数据。
        /// SkillHUD 和浮动文字可通过此事件响应技能释放。
        /// </summary>
        public event Action<SkillData> OnSkillActivated;

        /// <summary>
        /// 技能升级事件：参数为升级后的技能数据和新等级。
        /// </summary>
        public event Action<SkillData, int> OnSkillUpgraded;

        /// <summary>
        /// Buff 过期事件：参数为 Buff 失效的技能数据。
        /// </summary>
        public event Action<SkillData> OnBuffExpired;

        private float lastRefreshTime;

        // 冲刺无敌帧恢复相关字段（不在 Tick 中额外操作，由 Tick 中的 Dash 恢复逻辑统一处理）
        private bool pendingDashRestore;
        private float dashRestoreTime;
        private float dashOriginalInvincibility;
        private Player dashPlayer;

        /// <summary>
        /// 初始化技能系统，注册4个默认技能。
        /// 重复调用会跳过已初始化状态。
        /// </summary>
        public void Initialize()
        {
            if (this.IsInitialized)
            {
                return;
            }

            this.Skills = new List<SkillData>
            {
                SkillData.CreateWhirlwind(),
                SkillData.CreateDash(),
                SkillData.CreatePowerSurge(),
                SkillData.CreateHealingLight(),
            };

            this.AvailableUpgradePoints = 0;
            this.IsInitialized = true;
            this.lastRefreshTime = Time.time;

            EventBus.Instance.Subscribe<PlayerSkillActivatedEvent>(this.OnPlayerSkillActivated);
        }

        /// <summary>
        /// 尝试激活指定槽位的技能。
        /// 校验顺序：初始化状态 → 槽位有效性 → 冷却就绪 → 法力充足 → 玩家存活 → 执行技能效果。
        /// </summary>
        /// <param name="slotIndex">技能槽位索引（0-3）</param>
        /// <returns>是否成功激活</returns>
        public bool TryActivateSkill(ActivateSkillCommand command)
        {
            if (command == null)
            {
                return false;
            }

            return this.TryActivateSkill(command.SlotIndex);
        }

        private void OnPlayerSkillActivated(PlayerSkillActivatedEvent e)
        {
            this.TryActivateSkill(e.SlotIndex);
        }

        public bool TryActivateSkill(int slotIndex)
        {
            if (!this.IsInitialized)
            {
                return false;
            }

            if (slotIndex < 0 || slotIndex >= this.Skills.Count)
            {
                return false;
            }

            SkillData skill = this.Skills[slotIndex];
            Player player = PlayerManager.Instance?.Mine;
            if (player == null || player.CharacterDataLAB == null || player.CharacterDataLAB.Hp <= 0)
            {
                return false;
            }

            // 冷却检查
            if (!skill.IsReadyAt(Time.time))
            {
                return false;
            }

            // 法力检查
            GameCharacter.CharacterData playerData = player.CharacterDataLAB;
            if (!SkillTool.HasEnoughMana(playerData.Mp, skill.ManaCost))
            {
                return false;
            }

            // 消耗法力
            playerData.Mp -= skill.ManaCost;

            // 刷新玩家状态UI — 通过 Player.RefreshUI() 发布事件，不再直接操作 PlayerStatusUI
            PlayerManager.Instance.Mine?.RefreshUI();

            // 记录激活时间
            skill.LastActivateTime = Time.time;

            // 根据技能类型执行效果
            this.ExecuteSkillEffect(skill, player);

            // 触发激活事件
            this.OnSkillActivated?.Invoke(skill);

            return true;
        }

        /// <summary>
        /// 尝试升级指定技能。
        /// 消耗1个可用升级点数，提升技能等级1级。
        /// </summary>
        /// <param name="slotIndex">技能槽位索引（0-3）</param>
        /// <returns>是否成功升级</returns>
        public bool TryUpgradeSkill(int slotIndex)
        {
            if (!this.IsInitialized)
            {
                return false;
            }

            if (slotIndex < 0 || slotIndex >= this.Skills.Count)
            {
                return false;
            }

            SkillData skill = this.Skills[slotIndex];
            if (skill.IsMaxLevel)
            {
                return false;
            }

            int cost = SkillTool.GetUpgradeCost(skill.Level);
            if (this.AvailableUpgradePoints < cost)
            {
                return false;
            }

            this.AvailableUpgradePoints -= cost;
            int oldLevel = skill.Level;
            skill.Level++;
            this.OnSkillUpgraded?.Invoke(skill, skill.Level);

            // 升级后若技能正在冷却中，不重置冷却
            return true;
        }

        /// <summary>
        /// 为玩家增加可用的技能升级点数。
        /// 通常在击杀 Boss 或完成波次时调用。
        /// </summary>
        /// <param name="points">增加的点数</param>
        public void AddUpgradePoints(int points)
        {
            if (points <= 0)
            {
                return;
            }

            this.AvailableUpgradePoints += points;
        }

        /// <summary>
        /// 获取指定槽位的技能数据。
        /// </summary>
        /// <param name="slotIndex">槽位索引（0-3）</param>
        /// <returns>技能数据，无效索引返回 null</returns>
        public SkillData GetSkill(int slotIndex)
        {
            if (!this.IsInitialized || slotIndex < 0 || slotIndex >= this.Skills.Count)
            {
                return null;
            }

            return this.Skills[slotIndex];
        }

        /// <summary>
        /// 获取当前激活的 Buff 攻击力倍率（叠加多个 Buff 取最大值）。
        /// 在 Character.ReduceHp 伤害计算前调用，实现技能 Buff 对伤害的增强。
        /// </summary>
        /// <returns>当前 Buff 倍率，无 Buff 时返回 1.0</returns>
        public float GetActiveAttackBuffMultiplier()
        {
            if (!this.IsInitialized)
            {
                return 1.0f;
            }

            float maxBuff = 1.0f;
            foreach (SkillData skill in this.Skills)
            {
                if (skill.IsBuffActiveAt(Time.time) && skill.GetCurrentBuffMultiplier(Time.time) > maxBuff)
                {
                    maxBuff = skill.GetCurrentBuffMultiplier(Time.time);
                }
            }

            return maxBuff;
        }

        /// <summary>
        /// 每帧更新：检查 Buff 过期、冷却就绪事件。
        /// 通过 GlobalInit.Update 驱动，避免新增 MonoBehaviour。
        /// </summary>
        public void Tick()
        {
            if (!this.IsInitialized)
            {
                return;
            }

            float now = Time.time;

            // 节流：按 HUD 刷新间隔检查事件
            if (now - this.lastRefreshTime < SkillConstant.HudRefreshInterval)
            {
                return;
            }

            this.lastRefreshTime = now;

            foreach (SkillData skill in this.Skills)
            {
                // 检查 Buff 过期
                if (skill.EffectType == SkillEffectType.AttackBuff
                    && skill.BuffEndTime > 0f
                    && now >= skill.BuffEndTime)
                {
                    skill.BuffEndTime = 0f;
                    this.OnBuffExpired?.Invoke(skill);
                }
            }

            // 冲刺无敌帧恢复检查
            if (this.pendingDashRestore && now >= this.dashRestoreTime)
            {
                this.pendingDashRestore = false;
                if (this.dashPlayer != null)
                {
                    this.dashPlayer.InvincibilityDuration = this.dashOriginalInvincibility;
                }
            }
        }

        /// <summary>
        /// 执行技能的具体游戏效果。
        /// 根据 SkillType 和 SkillEffectType 分派到不同处理逻辑。
        /// </summary>
        private void ExecuteSkillEffect(SkillData skill, Player player)
        {
            switch (skill.SkillType)
            {
                case SkillType.SelfAOE:
                    this.ExecuteSelfAOE(skill, player);
                    break;
                case SkillType.Movement:
                    this.ExecuteMovement(skill, player);
                    break;
                case SkillType.SelfBuff:
                    this.ExecuteSelfBuff(skill, player);
                    break;
                case SkillType.SelfHeal:
                    this.ExecuteSelfHeal(skill, player);
                    break;
            }
        }

        /// <summary>
        /// 执行自身范围攻击：查询半径内敌人并对每个敌人造成伤害。
        /// </summary>
        private void ExecuteSelfAOE(SkillData skill, Player player)
        {
            float damage = SkillTool.CalculateSkillDamage(
                player.CharacterDataLAB.ATN, skill.EffectMultiplier, skill.Level);

            // 应用当前攻击 Buff（如果有的话，取其他Buff的加成）
            // 注意：这里不叠加自己的Buff，因为力量爆发可能还未激活

            List<AEnemy> enemies = SkillTool.GetEnemiesInRadius(
                player.transform.position, skill.AoeRadius);

            foreach (AEnemy enemy in enemies)
            {
                if (enemy != null && enemy.CharacterDataLAB != null && enemy.CharacterDataLAB.Hp > 0)
                {
                    // AOE 技能使用技能伤害，绕过普通攻击的暴击判定
                    // 应用当前所有活跃 Buff 的加成
                    float buffMult = this.GetActiveAttackBuffMultiplier();
                    float finalDamage = damage * buffMult;

                    // 对敌人施加防御减免
                    float defReduction = finalDamage * enemy.CharacterDataLAB.DEF / 10f;
                    finalDamage -= defReduction;
                    finalDamage = System.Math.Max(1f, finalDamage);

                    // 通过 Character.ReduceHp 走标准伤害管道（含浮动文字、统计等）
                    enemy.ReduceHp(finalDamage, player, false);

                    // 生成技能伤害浮动文字（复用现有伤害文字系统）
                    FloatingTextManager.Instance?.SpawnDamageText(
                        enemy.transform.position, finalDamage, false, false);
                }
            }
        }

        /// <summary>
        /// 执行位移技能：向玩家朝向方向瞬移一段距离，并附加短暂无敌帧。
        /// 使用 Rigidbody2D.MovePosition 实现平滑位移，避免穿透碰撞。
        /// 无敌帧持续时间通过 Timer 在 Tick 中恢复，不依赖协程。
        /// </summary>
        private void ExecuteMovement(SkillData skill, Player player)
        {
            // 获取玩家移动方向（从 Animator Direction 参数推断）
            Vector3 dashDirection = this.GetPlayerFacingDirection(player);
            Vector3 targetPosition = player.transform.position + (dashDirection * skill.AoeRadius);

            // 使用 MovePosition 实现位移（如果有 Rigidbody2D）
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.MovePosition(targetPosition);
            }
            else
            {
                player.transform.position = targetPosition;
            }

            // 临时延长无敌帧持续时间，记录恢复时间和原始值
            this.pendingDashRestore = true;
            this.dashRestoreTime = Time.time + SkillConstant.DashInvincibilityDuration;
            this.dashOriginalInvincibility = player.InvincibilityDuration;
            this.dashPlayer = player;
            player.InvincibilityDuration = System.Math.Max(
                player.InvincibilityDuration, SkillConstant.DashInvincibilityDuration);
        }

        /// <summary>
        /// 执行自身增益：设置 Buff 结束时间，后续伤害计算会自动读取。
        /// </summary>
        private void ExecuteSelfBuff(SkillData skill, Player player)
        {
            skill.BuffEndTime = Time.time + skill.BuffDuration;
        }

        /// <summary>
        /// 执行自身治疗：回复玩家生命值。
        /// </summary>
        private void ExecuteSelfHeal(SkillData skill, Player player)
        {
            float healAmount = SkillTool.CalculateHealAmount(skill.EffectMultiplier, skill.Level);
            player.AddHp(healAmount);

            // 生成治疗浮动文字
            FloatingTextManager.Instance?.SpawnHealText(player.transform.position, healAmount);
        }

        /// <summary>
        /// 获取玩家当前朝向方向（基于 Animator Direction 参数）。
        /// 上=0: (0,1)，下=1: (0,-1)，左=2: (-1,0)，右=3: (1,0)。
        /// 默认返回右方向。
        /// </summary>
        private Vector3 GetPlayerFacingDirection(Player player)
        {
            Animator animator = player.GetComponent<Animator>();
            if (animator == null)
            {
                return Vector3.right;
            }

            int dir = animator.GetInteger("Direction");
            return dir switch
            {
                0 => Vector3.up,
                1 => Vector3.down,
                2 => Vector3.left,
                3 => Vector3.right,
                _ => Vector3.right,
            };
        }

    }
}
