namespace LAB2D.Character.Player
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.Domain.Character;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Player;
    using LAB2D.UnityAdapter;
    using System;
    using System.Collections.Generic;
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 渲染更新 Update,不受Time.timeScale影响;Time.deltaTime,受Time.timeScale影响
    /// 物理更新 FixedUpdate,受Time.timeScale影响.
    /// 动作: Direction -> 0-Up 1-Right 2-Down 3-Left Action -> 0-Walk 1-Run 2-Idle
    /// </summary>
    public class Player : Character
    {
        private Rigidbody2D rg;
        private IPlayerView playerView;

        /// <inheritdoc/>
        public override bool IsPlayerCharacter => true;
        private readonly PlayerDamagePolicy damagePolicy = new PlayerDamagePolicy();
        private readonly PlayerMovementPolicy movementPolicy = new PlayerMovementPolicy();
        private readonly PlayerMovementService movementService = new PlayerMovementService();

        // --- 可替换的 Provider 委托（默认指向 MonoBehaviour 单例，测试可覆盖） ---
        internal static Func<bool> IsRespawningProvider { get; set; } = () => ServiceLocator.Get<DeathPenaltyManager>().IsRespawning;
        internal static Action UpdateDeathScreenProvider { get; set; } = () => ServiceLocator.Get<DeathPenaltyManager>().UpdateDeathScreen();
        internal static Func<Player, bool> TryCompleteRespawnProvider { get; set; } = (p) => ServiceLocator.Get<DeathPenaltyManager>().TryCompleteRespawn(p);
        internal static Action<Player> HandlePlayerDeathProvider { get; set; } = (p) => ServiceLocator.Get<DeathPenaltyManager>().HandlePlayerDeath(p);
        internal static Func<Player, float, float> WeatherMoveSpeedProvider { get; set; } = (p, def) => ServiceLocator.Get<WeatherGameplayEffect>().GetAdjustedCharacterMoveSpeed(p, def);
        internal static Func<Player, float, float> WaveMoveSpeedProvider { get; set; } = (p, def) => ServiceLocator.Get<WaveBossRewardManager>().GetAdjustedPlayerMoveSpeed(p, def);
        internal static Func<float> ExperienceMultiplierProvider { get; set; } = () => ServiceLocator.Get<ComboBonusManager>().ExperienceMultiplier;
        internal static Action<Player> PlayerRegisterProvider { get; set; } = (p) => ServiceLocator.Get<PlayerManager>().Mine = p;
        internal static Action<Player> PlayerAddProvider { get; set; } = (p) => ServiceLocator.Get<PlayerManager>().Add(p);
        internal static Action<Player> PlayerRemoveProvider { get; set; } = (p) => ServiceLocator.Get<PlayerManager>().Remove(p);
        internal static Action PlayerDeathRecordProvider { get; set; } = () => ServiceLocator.Get<GameplaySessionStats>().RecordPlayerDeath();
        internal static Action<ABackpackItem> BackpackSaveProvider { get; set; } = (item) => ServiceLocator.Get<BackpackController>().AddItem(item);

        /// <summary>
        /// 死亡时层切换提供者 — 玩家死亡时暂时切换到 Default 层以躲避敌人。
        /// 默认实现直接操作 gameObject.layer。
        /// 可在测试中替换为无操作桩。
        /// </summary>
        internal static Action<Player> DeathLayerSwitchProvider { get; set; }
            = (player) =>
            {
                if (player == null)
                {
                    return;
                }

                player.gameObject.layer = LayerMask.NameToLayer("Default");
            };

        /// <summary>
        /// 本地玩家 TagObject 设置提供者 — 将 Player 实例绑定到 Photon LocalPlayer.TagObject。
        /// 默认实现使用 PhotonNetwork.LocalPlayer。
        /// 可在测试中替换为无操作桩。
        /// </summary>
        internal static Action<Player> LocalPlayerTagObjectProvider { get; set; }
            = (player) =>
            {
                if (player == null)
                {
                    return;
                }

                PhotonNetwork.LocalPlayer.TagObject = player;
            };

        /// <summary>
        /// 本地玩家昵称提供者 — 获取 Photon 本地玩家的昵称。
        /// 默认实现使用 PhotonNetwork.NickName。
        /// 可在测试或离线模式中替换。
        /// </summary>
        internal static Func<string> LocalPlayerNameProvider { get; set; }
            = () => PhotonNetwork.NickName;

        // --- Unity 组件初始化 Provider（可替换为测试桩，隔离 UnityEngine 依赖） ---

        /// <summary>
        /// Rigidbody2D 初始化提供者 — 获取并配置玩家的 Rigidbody2D 组件。
        /// 默认实现设置 freezeRotation=true 和 interpolation=Interpolate。
        /// 可在测试中替换为返回桩 Rigidbody2D 的实现。
        /// </summary>
        internal static Func<Player, Rigidbody2D> RigidbodySetupProvider { get; set; }
            = (player) =>
            {
                Rigidbody2D rg = player.GetComponent<Rigidbody2D>();
                rg.freezeRotation = true;
                rg.interpolation = RigidbodyInterpolation2D.Interpolate;
                return rg;
            };

        /// <summary>
        /// Animator 组件提供者 — 获取玩家身上的 Animator 组件。
        /// 默认实现使用 GetComponent&lt;Animator&gt;。
        /// 可在测试中替换为返回桩 Animator 的实现。
        /// </summary>
        internal static Func<Player, Animator> AnimatorProvider { get; set; }
            = (player) => player.GetComponent<Animator>();

        /// <summary>
        /// 主摄像机提供者 — 获取 Camera.main 上的 CameraMove 组件并初始化位置。
        /// 默认实现从 Camera.main 获取。
        /// 可在测试中替换。
        /// </summary>
        internal static Func<Player, CameraMove> MainCameraProvider { get; set; }
            = (player) =>
            {
                CameraMove cam = Camera.main != null
                    ? Camera.main.GetComponent<CameraMove>()
                    : null;
                if (cam != null)
                {
                    cam.DirectToPosition(player.transform.position);
                }

                return cam;
            };

        /// <summary>
        /// 小地图摄像机提供者 — 通过 Minimap 标签查找 CameraMove 组件并初始化位置。
        /// 默认实现使用 GameObject.FindGameObjectWithTag + GetComponent。
        /// 可在测试中替换。
        /// </summary>
        internal static Func<Player, CameraMove> MiniCameraProvider { get; set; }
            = (player) =>
            {
                GameObject miniObj = GameObject.FindGameObjectWithTag(TagConstant.MINIMAP_TAG);
                CameraMove cam = miniObj != null
                    ? miniObj.GetComponent<CameraMove>()
                    : null;
                if (cam != null)
                {
                    cam.DirectToPosition(player.transform.position);
                }

                return cam;
            };

        /// <summary>
        /// 玩家名字显示提供者 — 在玩家 GameObject 的 "Name" 子节点上显示名字文本。
        /// 默认实现使用 Tool.GetComponentInChildren&lt;Text&gt; 查找并设置文本。
        /// 可在测试中替换为无操作桩。
        /// </summary>
        internal static Action<Player, string> PlayerNameDisplayProvider { get; set; }
            = (player, name) =>
            {
                if (player == null)
                {
                    return;
                }

                Text nameText = LAB2D.Tool.Tool.GetComponentInChildren<Text>(player.gameObject, "Name");
                if (nameText != null)
                {
                    nameText.text = name;
                }
            };

        /// <summary>
        /// 奔跑速度倍率，默认1.6倍
        /// </summary>
        private float runSpeedMultiplier = 1.6f;

        /// <summary>
        /// 奔跑速度倍率，可运行时动态调整
        /// </summary>
        public float RunSpeedMultiplier
        {
            get => this.runSpeedMultiplier;
            set => this.runSpeedMultiplier = this.movementPolicy.ClampRunSpeedMultiplier(value);
        }

        /// <summary>
        /// 受击无敌帧持续时间（秒），默认0.5秒，设为0可禁用无敌帧
        /// </summary>
        private float invincibilityDuration = 0.5f;

        /// <summary>
        /// 上次受到伤害的时间（Time.time），用于无敌帧判定
        /// </summary>
        private float lastDamageTime = -99f;

        /// <summary>
        /// 受击无敌帧持续时间（秒），可运行时动态调整
        /// </summary>
        public float InvincibilityDuration
        {
            get => this.invincibilityDuration;
            set => this.invincibilityDuration = this.damagePolicy.ClampInvincibilityDuration(value);
        }

        /// <summary>
        /// 当前是否处于受击无敌状态（伤害冷却中）
        /// </summary>
        public bool IsInvincible
        {
            get
            {
                return this.damagePolicy.IsInvincible(
                    this.gameTime.Time,
                    this.lastDamageTime,
                    this.invincibilityDuration);
            }
        }

        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();
            this.name = "Player";
            this.basicAttribute = new Attribute(1.0f, 1.0f, 1.0f, 1.0f, 0.05f, 1.0f, 1.0f, 1.0f);
            this.CharacterDataLAB = new PlayerData();
            this.CharacterDataLAB.Character = this;
            this.AttackLayers = LayerMask.GetMask("Tile", LayerConstant.ENEMY_LAYER, LayerConstant.WORKER_LAYER);
            this.AttackTags = new List<string>
            {
                "Enemy",
                "Worker",
            };
            this.rg = RigidbodySetupProvider(this);
        }

        /// <inheritdoc/>
        public override void Start()
        {
            base.Start();

            Animator animator = AnimatorProvider(this);
            if (animator == null)
            {
                AWorkerTask.LogProvider("animator Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            // 不在线，或者在线并且是自己
            if (this.NetworkView.IsMine || !this.NetworkView.IsOnline)
            {
                this.MoveSpeed = 5;
                CameraMove miniCamera = MiniCameraProvider(this);
                CameraMove mainCamera = MainCameraProvider(this);

                // 创建表现层适配器，注入所有 Unity 表现组件
                this.playerView = new PlayerViewAdapter(
                    animator,
                    this.rg,
                    this.spriteRenderer,
                    this.transform,
                    mainCamera,
                    miniCamera,
                    this,
                    this.originalColor);

                PlayerRegisterProvider(this);
                LocalPlayerTagObjectProvider(this);
                PlayerNameDisplayProvider(this, LocalPlayerNameProvider());
                PlayerData playerData = this.CharacterDataLAB as PlayerData;
                this.RefreshUI();
            }
            else if (!this.NetworkView.IsMine)
            {
                PlayerNameDisplayProvider(this, this.NetworkView.OwnerName);
                PlayerAddProvider(this);

                // PhotonNetwork.PlayerList[PhotonNetwork.PlayerList.Length - 1].TagObject = this;
            }
        }

        public void Update()
        {
            // 复活等待期间：显示倒计时，阻止所有操作
            if (IsRespawningProvider())
            {
                UpdateDeathScreenProvider();
                return;
            }

            // 计时器刚到期：完成复活（移动到随机位置，恢复 HP/MP，隐藏死亡界面）
            if (TryCompleteRespawnProvider(this))
            {
                this.RefreshUI();
            }

            this.Attack();

            // A008: 主动技能系统 — 检测技能快捷键并激活
            this.HandleSkillInput();
        }

        public void FixedUpdate()
        {
            // 如果观察的当期的角色并且连接服务器,防止误操作别的玩家
            if (this.NetworkView.IsOnline && !this.NetworkView.IsMine)
            {
                return;
            }

            // 复活期间阻止移动
            if (!IsRespawningProvider())
            {
                this.Move();
            }

            // 表现层每帧更新（受击闪烁计时器、边缘特效等）
            this.playerView?.Tick(Time.fixedDeltaTime);
        }

        /// <inheritdoc/>
        public override void Attack()
        {
            if (UnityPlayerInputAdapter.GetPointerAttackDown(this.GetInstanceID(), out PlayerAttackCommand command))
            {
                this.HandleAttackCommand(command);
            }
        }

        private void HandleAttackCommand(PlayerAttackCommand command)
        {
            if (command == null)
            {
                return;
            }

            EventBusPublishProvider(new PlayerAttackRequestedEvent { EntityId = command.EntityId });
        }

        /// <inheritdoc/>
        public override void AddExperienceValue(int experience)
        {
            if (experience > 0)
            {
                experience = MathHelper.RoundToInt(experience * ExperienceMultiplierProvider());
            }

            base.AddExperienceValue(experience);
            this.RefreshUI();
        }

        /// <summary>
        /// 加血.
        /// </summary>
        /// <param name="hp">血量.</param>
        public void AddHp(float hp)
        {
            CharacterRuntimeState state = CharacterRuntimeState.FromCharacterData(
                this.CharacterDataLAB.Hp,
                this.CharacterDataLAB.MaxHp,
                this.CharacterDataLAB.Mp,
                this.CharacterDataLAB.MaxMp,
                this.CharacterDataLAB.Level,
                this.CharacterDataLAB.CurExperience,
                this.CharacterDataLAB.MaxExperience,
                this.lastDamageTime,
                IsRespawningProvider());

            CharacterRuntimeState newState = this.healthComponent.ApplyHealingToState(state, hp);
            this.CharacterDataLAB.Hp = newState.Hp;

            this.RefreshUI();
        }

        /// <inheritdoc/>
        public override void ReduceHp(float hp, Character attacker, bool isCRT = false)
        {
            if (hp <= 0)
            {
                AWorkerTask.LogProvider("Hp can't less than zero!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            if (this.damagePolicy.ShouldIgnoreDamage(
                hp,
                IsRespawningProvider(),
                this.gameTime.Time,
                this.lastDamageTime,
                this.invincibilityDuration))
            {
                return;
            }

            CharacterRuntimeState state = CharacterRuntimeState.FromCharacterData(
                this.CharacterDataLAB.Hp,
                this.CharacterDataLAB.MaxHp,
                this.CharacterDataLAB.Mp,
                this.CharacterDataLAB.MaxMp,
                this.CharacterDataLAB.Level,
                this.CharacterDataLAB.CurExperience,
                this.CharacterDataLAB.MaxExperience,
                this.lastDamageTime,
                IsRespawningProvider());

            CharacterHealthDamageResult damageResult = this.healthComponent.ApplyDamageToState(
                state,
                this.CharacterDataLAB.DEF,
                hp,
                this.gameTime.Time,
                this.invincibilityDuration,
                isPlayer: true,
                attackerIsPlayer: false,
                isCRT,
                this.damagePolicy,
                attackerCharacter: attacker,
                targetCharacter: this);

            this.CharacterDataLAB.Hp = damageResult.NewState.Hp;
            this.lastDamageTime = damageResult.NewState.LastDamageTime;

            if (damageResult.WasBlocked)
            {
                return;
            }

            this.playerView?.PlayHitFlash();

            float worldPosX = this.transform.position.x;
            float worldPosY = this.transform.position.y;
            EventBusPublishProvider(new CharacterDamagedEvent
            {
                TargetId = this.CharacterDataLAB.Id,
                AttackerId = attacker?.CharacterDataLAB?.Id ?? 0,
                Damage = damageResult.FinalDamage,
                IsCritical = isCRT,
                IsCombo = false,
                RemainingHp = this.CharacterDataLAB.Hp,
                WorldPosX = worldPosX,
                WorldPosY = worldPosY,
            });

            if (damageResult.NewState.IsDead)
            {
                this.Death();
            }

            if (this.NetworkView.IsOnline && !this.NetworkView.IsMine)
            {
                return;
            }

            this.RefreshUI();
        }

        /// <summary>
        /// 发布玩家状态变化事件，通知 UI 层刷新。
        /// 游戏逻辑层修改玩家状态后调用此方法，不再直接操作 PlayerStatusUI。
        /// </summary>
        public void RefreshUI()
        {
            PlayerData playerData = this.CharacterDataLAB as PlayerData;
            if (playerData == null)
            {
                return;
            }

            EventBusPublishProvider(new PlayerStatusChangedEvent
            {
                Hp = this.CharacterDataLAB.Hp,
                MaxHp = this.CharacterDataLAB.MaxHp,
                Mp = playerData.Mp,
                MaxMp = playerData.MaxMp,
                Level = playerData.Level,
                CurExperience = playerData.CurExperience,
                MaxExperience = playerData.MaxExperience,
            });
        }

        /// <summary>
        /// 某位置是否在玩家周围（世界坐标版本）。
        /// 已废弃：请使用 <see cref="IsArround(GameGridPosition)"/> 替代。
        /// </summary>
        /// <param name="pos">世界坐标位置.</param>
        /// <returns>是否.</returns>
        [System.Obsolete("Use IsArround(GameGridPosition) instead.")]
        public bool IsArround(Vector3 pos)
        {
            if (pos == null)
            {
                AWorkerTask.LogProvider("pos is null!!!", LogManager.LogLevelEnum.Error);
                return false;
            }

            return pos.x < this.transform.position.x + 50 &&
                pos.x > this.transform.position.x - 50 &&
                pos.y > this.transform.position.y - 50 &&
                pos.y < this.transform.position.y + 50;
        }

        /// <summary>
        /// 某网格位置是否在玩家周围。
        /// 使用 Domain 层 GameGridPosition 替代 UnityEngine.Vector3。
        /// </summary>
        /// <param name="pos">网格位置.</param>
        /// <param name="range">判定范围（网格单位），默认 50.</param>
        /// <returns>是否在范围内.</returns>
        public bool IsArround(GameGridPosition pos, int range = 50)
        {
            Vector3Int playerMapPos = AWorkerTask.TileMapWorldToMapProvider(this.transform.position);
            int dx = pos.X - playerMapPos.x;
            int dy = pos.Y - playerMapPos.y;
            return dx > -range && dx < range && dy > -range && dy < range;
        }

        /// <summary>
        /// 切换角色视角.
        /// </summary>
        /// <param name="is_2_5D">是否是2.5D视角.</param>
        public void TogglePerspective(bool is_2_5D)
        {
            this.playerView?.TogglePerspective(is_2_5D);
        }

        /// <inheritdoc/>
        protected override void Death()
        {
            PlayerDeathRecordProvider();
            AWorkerTask.LogProvider("玩家死亡", LogManager.LogLevelEnum.Trace);
            this.CharacterDataLAB.Hp = 1; // 保持 1 HP 存活，防止复活期间再次死亡

            // 暂时切换 Player 层以躲避敌人
            DeathLayerSwitchProvider(this);

            HandlePlayerDeathProvider(this);

            // 自动采集会话结算数据（F011：补齐结算系统缺失的自动触发链路）
            SessionResultAutoTrigger.NotifyPlayerDeath();
        }

        /// <summary>
        /// A008: 处理主动技能快捷键输入。
        /// Q/E/R/F 分别对应技能槽位 0/1/2/3。
        /// 仅在非 UI 输入模式下生效，不干扰文本输入或面板操作。
        /// </summary>
        private void HandleSkillInput()
        {
            for (int slotIndex = 0; slotIndex < 4; slotIndex++)
            {
                if (UnityPlayerInputAdapter.GetGameplaySkillDown(
                    this.GetInstanceID(),
                    slotIndex,
                    out ActivateSkillCommand command))
                {
                    EventBusPublishProvider(new PlayerSkillActivatedEvent
                    {
                        EntityId = command.EntityId,
                        SlotIndex = command.SlotIndex,
                    });
                    return;
                }
            }
        }

        /// <summary>
        /// 玩家移动.
        /// </summary>
        private void Move()
        {
            PlayerMoveCommand command = UnityPlayerInputAdapter.PollCurrentPlayerMoveCommand(this.GetInstanceID(), this.gameTime.DeltaTime);
            if (command != null)
            {
                this.playerView?.EnsureCameraFollow(new GameVector2(this.transform.position.x, this.transform.position.y));
                bool isRunning = command.IsRunning;

                float weatherMultiplier = WeatherMoveSpeedProvider(this, 1.0f);
                // A004：波间奖励移动强化在天气倍率之后应用，避免覆盖天气玩法的减速/增益。
                float waveMultiplier = WaveMoveSpeedProvider(this, 1.0f);
                PlayerMoveResult moveResult = this.movementService.CalculateMovement(
                    this.MoveSpeed,
                    this.runSpeedMultiplier,
                    isRunning,
                    weatherMultiplier,
                    waveMultiplier,
                    command.Direction);

                this.playerView?.ApplyMoveAnimation(command, moveResult);
            }
            else
            {
                this.playerView?.ApplyIdleAnimation();
            }
        }

        private void OnDestroy()
        {
            PlayerRemoveProvider(this);

            // 关闭游戏添加正在装备的武器
            PlayerData playerData = this.CharacterDataLAB as PlayerData;
            if (playerData.Weapon != null)
            {
                BackpackSaveProvider(playerData.Weapon);
            }
        }

        /// <summary>
        /// 都有碰撞器,其中之一勾选Is Trigger,其中之一带有刚体
        /// </summary>
        /// <param name="collider">collider</param>
        // private void OnTriggerEnter2D(Collider2D collider)
        // {
        //     if (collider.gameObject.CompareTag("Enemy"))
        //     {
        //     }
        // }

        /// <summary>
        /// 都有碰撞器,其中之一带有刚体,都不勾选Is Trigger
        /// </summary>
        // private void OnColisionEnter2D(Collision2D collision) {
        //     //collision.contacts[0].point; // 碰撞的第一个点
        //     //collision.contacts[0].normal; // 碰撞的法线
        // }

        /// <summary>
        /// 敌人数据
        /// </summary>
        [Serializable]
        public class PlayerData : CharacterData
        {
        }
    }
}
