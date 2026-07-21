namespace LAB2D.Character.Player
{
    using LAB2D;
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
        private Animator animator;
        private Vector3 direction; // 按键玩家移动方向
        private CameraMove mainCamera;
        private CameraMove miniCamera;
        private SpriteRenderer sprite;
        private Rigidbody2D rg;
        private readonly PlayerDamagePolicy damagePolicy = new PlayerDamagePolicy();
        private readonly PlayerMovementPolicy movementPolicy = new PlayerMovementPolicy();
        private readonly PlayerMovementService movementService = new PlayerMovementService();

        // --- 可替换的 Provider 委托（默认指向 MonoBehaviour 单例，测试可覆盖） ---
        internal static Func<bool> IsRespawningProvider { get; set; } = () => DeathPenaltyManager.Instance.IsRespawning;
        internal static Action UpdateDeathScreenProvider { get; set; } = () => DeathPenaltyManager.Instance.UpdateDeathScreen();
        internal static Func<Player, bool> TryCompleteRespawnProvider { get; set; } = (p) => DeathPenaltyManager.Instance.TryCompleteRespawn(p);
        internal static Action<Player> HandlePlayerDeathProvider { get; set; } = (p) => DeathPenaltyManager.Instance.HandlePlayerDeath(p);
        internal static Func<Player, float, float> WeatherMoveSpeedProvider { get; set; } = (p, def) => WeatherGameplayEffect.Instance.GetAdjustedCharacterMoveSpeed(p, def);
        internal static Func<Player, float, float> WaveMoveSpeedProvider { get; set; } = (p, def) => WaveBossRewardManager.Instance.GetAdjustedPlayerMoveSpeed(p, def);
        internal static Func<float> ExperienceMultiplierProvider { get; set; } = () => ComboBonusManager.Instance.ExperienceMultiplier;
        internal static Action<Player> PlayerRegisterProvider { get; set; } = (p) => PlayerManager.Instance.Mine = p;
        internal static Action<Player> PlayerAddProvider { get; set; } = (p) => PlayerManager.Instance.Add(p);
        internal static Action<Player> PlayerRemoveProvider { get; set; } = (p) => PlayerManager.Instance.Remove(p);
        internal static Action PlayerDeathRecordProvider { get; set; } = () => GameplaySessionStats.Instance.RecordPlayerDeath();
        internal static Action<ABackpackItem> BackpackSaveProvider { get; set; } = (item) => BackpackController.Instance.AddItem(item);

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
            this.direction = default;
            if (this.direction == null)
            {
                AWorkerTask.LogProvider("direction assign resource Error!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            this.name = "Player";
            this.basicAttribute = new Attribute(1.0f, 1.0f, 1.0f, 1.0f, 0.05f, 1.0f, 1.0f, 1.0f);
            this.CharacterDataLAB = new PlayerData();
            this.CharacterDataLAB.Character = this;
            this.sprite = this.gameObject.GetComponent<SpriteRenderer>();
            this.AttackLayers = LayerMask.GetMask("Tile", LayerConstant.ENEMY_LAYER, LayerConstant.WORKER_LAYER);
            this.AttackTags = new List<string>
            {
                "Enemy",
                "Worker",
            };
            this.rg = this.GetComponent<Rigidbody2D>();
            this.rg.freezeRotation = true; // 防止旋转
            this.rg.interpolation = RigidbodyInterpolation2D.Interpolate; // 插值让移动更平滑，解决角色卡顿
        }

        /// <inheritdoc/>
        public override void Start()
        {
            base.Start();
            this.animator = this.GetComponent<Animator>();
            if (this.animator == null)
            {
                AWorkerTask.LogProvider("animator Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            // 不在线，或者在线并且是自己
            if (this.NetworkView.IsMine || !this.NetworkView.IsOnline)
            {
                this.MoveSpeed = 5;
                this.miniCamera = GameObject.FindGameObjectWithTag(TagConstant.MINIMAP_TAG).GetComponent<CameraMove>();
                this.miniCamera.DirectToPosition(this.transform.position);
                this.mainCamera = Camera.main.GetComponent<CameraMove>();
                this.mainCamera.DirectToPosition(this.transform.position);
                PlayerRegisterProvider(this);
                PhotonNetwork.LocalPlayer.TagObject = this;
                LAB2D.Tool.Tool.GetComponentInChildren<Text>(this.gameObject, "Name").text = PhotonNetwork.NickName;
                PlayerData playerData = this.CharacterDataLAB as PlayerData;
                this.RefreshUI();
            }
            else if (!this.NetworkView.IsMine)
            {
                LAB2D.Tool.Tool.GetComponentInChildren<Text>(this.gameObject, "Name").text = this.NetworkView.OwnerName;
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
            if (this.NetworkView.IsOnline && !this.NetworkView.IsMine && PhotonNetwork.IsConnected)
            {
                return;
            }

            // 复活期间阻止移动
            if (!IsRespawningProvider())
            {
                this.Move();
            }

            this.sprite.material.SetTexture("_MainTex", this.sprite.sprite.texture); // 设置边缘特效
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

            EventBus.Instance.Publish(new PlayerAttackRequestedEvent { EntityId = command.EntityId });
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

            this.spriteRenderer.color = Color.red;
            this.Invoke(nameof(this.ResetColor), 0.2f);

            EventBus.Instance.Publish(new CharacterDamagedEvent
            {
                TargetId = this.CharacterDataLAB.Id,
                AttackerId = attacker?.CharacterDataLAB?.Id ?? 0,
                Damage = damageResult.FinalDamage,
                IsCritical = isCRT,
                IsCombo = false,
                RemainingHp = this.CharacterDataLAB.Hp,
                WorldPosX = this.transform.position.x,
                WorldPosY = this.transform.position.y,
            });

            if (damageResult.NewState.IsDead)
            {
                this.Death();
            }

            if (this.NetworkView.IsOnline && !this.NetworkView.IsMine && PhotonNetwork.IsConnected)
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

            EventBus.Instance.Publish(new PlayerStatusChangedEvent
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
        /// 某位置是否在玩家周围.
        /// </summary>
        /// <param name="pos">位置.</param>
        /// <returns>是否.</returns>
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
        /// 切换角色视角.
        /// </summary>
        /// <param name="is_2_5D">是否是2.5D视角.</param>
        public void TogglePerspective(bool is_2_5D)
        {
            float rotationX = 0;
            if (is_2_5D)
            {
                rotationX = -45;
            }

            this.transform.rotation = Quaternion.Euler(rotationX, this.transform.rotation.y, this.transform.rotation.z);
            Camera.main.transform.rotation = Quaternion.Euler(new Vector3(rotationX, 0, 0));

            if (is_2_5D)
            {
                Camera.main.orthographic = false;
                Camera.main.fieldOfView = 100;
                this.mainCamera.Offset = new Vector3(0, -6, 14);
            }
            else
            {
                Camera.main.orthographic = true;
                Camera.main.orthographicSize = 10;
                this.mainCamera.Offset = new Vector3(0, 0, 0);
            }
        }

        /// <inheritdoc/>
        protected override void Death()
        {
            PlayerDeathRecordProvider();
            AWorkerTask.LogProvider("玩家死亡", LogManager.LogLevelEnum.Trace);
            this.CharacterDataLAB.Hp = 1; // 保持 1 HP 存活，防止复活期间再次死亡

            // 暂时切换 Player 层以躲避敌人
            this.gameObject.layer = LayerMask.NameToLayer("Default");

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
                    EventBus.Instance.Publish(new PlayerSkillActivatedEvent
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
                this.BindCameras();
                bool isRunning = command.IsRunning;
                this.direction.x = command.Direction.X;
                this.direction.y = command.Direction.Y;

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

                this.ApplyMovePresentation(command, moveResult);
            }
            else
            {
                this.ApplyIdlePresentation();
            }
        }

        private void BindCameras()
        {
            if (this.mainCamera.Character != this)
            {
                this.mainCamera.DirectToPosition(this.transform.position);
                this.mainCamera.Character = this;
            }

            if (this.miniCamera.Character != this)
            {
                this.miniCamera.DirectToPosition(this.transform.position);
                this.miniCamera.Character = this;
            }
        }

        private void ApplyMovePresentation(PlayerMoveCommand command, PlayerMoveResult moveResult)
        {
            this.animator.SetInteger("Action", command.IsRunning ? 1 : 0);

            int animDir;
            if (command.Direction.Y > 0)
            {
                animDir = 0;
            }
            else if (command.Direction.X > 0)
            {
                animDir = 1;
            }
            else if (command.Direction.Y < 0)
            {
                animDir = 2;
            }
            else
            {
                animDir = 3;
            }

            this.animator.SetInteger("Direction", animDir);
            this.rg.velocity = new Vector2(moveResult.Velocity.X, moveResult.Velocity.Y);
        }

        private void ApplyIdlePresentation()
        {
            this.animator.SetInteger("Action", 2);
            this.rg.velocity = Vector3.zero;
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
