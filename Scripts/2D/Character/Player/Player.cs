namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 渲染更新 Update,不受Time.timeScale影响;Time.deltaTime,受Time.timeScale影响
    /// 物理更新 FixedUpdate,受Time.timeScale影响.
    /// </summary>
    public class Player : Character
    {
        private Animator animator;
        private Vector3 direction; // 按键玩家移动方向
        private CameraMove mainCamera;
        private CameraMove miniCamera;
        private SpriteRenderer sprite;
        private Rigidbody2D rg;

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
            set => this.invincibilityDuration = Mathf.Max(0f, value);
        }

        /// <summary>
        /// 当前是否处于受击无敌状态（伤害冷却中）
        /// </summary>
        public bool IsInvincible
        {
            get
            {
                return this.invincibilityDuration > 0f &&
                       Time.time - this.lastDamageTime < this.invincibilityDuration;
            }
        }

        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();
            this.direction = default;
            if (this.direction == null)
            {
                LogManager.Instance.Log("direction assign resource Error!!!", LogManager.LogLevelEnum.Error);
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
                LogManager.Instance.Log("animator Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            // 不在线，或者在线并且是自己
            if (this.pv.IsMine || !NetworkConnect.Instance.IsOnline)
            {
                this.MoveSpeed = 5;
                this.miniCamera = GameObject.FindGameObjectWithTag(TagConstant.MINIMAP_TAG).GetComponent<CameraMove>();
                this.miniCamera.DirectToPosition(this.transform.position);
                this.mainCamera = Camera.main.GetComponent<CameraMove>();
                this.mainCamera.DirectToPosition(this.transform.position);
                PlayerManager.Instance.Mine = this;
                PhotonNetwork.LocalPlayer.TagObject = this;
                Tool.GetComponentInChildren<Text>(this.gameObject, "Name").text = PhotonNetwork.NickName;
                PlayerData playerData = this.CharacterDataLAB as PlayerData;
                PlayerStatusUI.Instance.UpdatePlayerState(
                    this.CharacterDataLAB.Hp,
                    this.CharacterDataLAB.MaxHp,
                    playerData.Mp,
                    playerData.MaxMp,
                    playerData.Level,
                    playerData.CurExperience,
                    playerData.MaxExperience);
            }
            else if (!this.pv.IsMine)
            {
                Tool.GetComponentInChildren<Text>(this.gameObject, "Name").text = this.pv.Owner.NickName;
                PlayerManager.Instance.Add(this);

                // PhotonNetwork.PlayerList[PhotonNetwork.PlayerList.Length - 1].TagObject = this;
            }
        }

        public void Update()
        {
            // During respawn wait: show countdown, block all actions
            if (DeathPenaltyManager.Instance.IsRespawning)
            {
                DeathPenaltyManager.Instance.UpdateDeathScreen();
                return;
            }

            // Timer just expired: complete respawn (move to random pos, restore HP/MP, hide death screen)
            if (DeathPenaltyManager.Instance.TryCompleteRespawn(this))
            {
                PlayerData playerData = this.CharacterDataLAB as PlayerData;
                PlayerStatusUI.Instance.UpdatePlayerState(
                    this.CharacterDataLAB.Hp,
                    this.CharacterDataLAB.MaxHp,
                    playerData.Mp,
                    playerData.MaxMp,
                    playerData.Level,
                    playerData.CurExperience,
                    playerData.MaxExperience);
            }

            this.Attack();
        }

        public void FixedUpdate()
        {
            // 如果观察的当期的角色并且连接服务器,防止误操作别的玩家
            if (NetworkConnect.Instance.IsOnline && !this.pv.IsMine && PhotonNetwork.IsConnected)
            {
                return;
            }

            // Block movement while respawning
            if (!DeathPenaltyManager.Instance.IsRespawning)
            {
                this.Move();
            }

            this.sprite.material.SetTexture("_MainTex", this.sprite.sprite.texture); // 设置边缘特效
        }

        /// <inheritdoc/>
        public override void Attack()
        {
            if (Input.GetMouseButtonDown(0))
            {
                ForegroundPanel.Instance.Onclick_Attack();
            }
        }

        /// <inheritdoc/>
        public override void AddExperienceValue(int experience)
        {
            // 连击增益：经验值加成（连击越高，经验倍率越大）
            if (experience > 0)
            {
                experience = Mathf.RoundToInt(experience * ComboBonusManager.Instance.ExperienceMultiplier);
            }

            base.AddExperienceValue(experience);
            PlayerStatusUI.Instance.UpdatePlayerState(
                this.CharacterDataLAB.Hp,
                this.CharacterDataLAB.MaxHp,
                this.CharacterDataLAB.Mp,
                this.CharacterDataLAB.MaxMp,
                this.CharacterDataLAB.Level,
                this.CharacterDataLAB.CurExperience,
                this.CharacterDataLAB.MaxExperience);
        }

        /// <summary>
        /// 加血.
        /// </summary>
        /// <param name="hp">血量.</param>
        public void AddHp(float hp)
        {
            this.CharacterDataLAB.Hp += hp;
            if (this.CharacterDataLAB.Hp > this.CharacterDataLAB.MaxHp)
            {
                this.CharacterDataLAB.Hp = this.CharacterDataLAB.MaxHp;
            }

            PlayerData playerData = this.CharacterDataLAB as PlayerData;
            PlayerStatusUI.Instance.UpdatePlayerState(
                this.CharacterDataLAB.Hp,
                this.CharacterDataLAB.MaxHp,
                playerData.Mp,
                playerData.MaxMp,
                playerData.Level,
                playerData.CurExperience,
                playerData.MaxExperience);
        }

        /// <inheritdoc/>
        public override void ReduceHp(float hp, Character attacker, bool isCRT = false)
        {
            if (hp <= 0)
            {
                LogManager.Instance.Log("Hp can't less than zero!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            // Invulnerable during respawn period
            if (DeathPenaltyManager.Instance.IsRespawning)
            {
                return;
            }

            // 受击无敌帧保护：在无敌时间窗口内忽略所有伤害，防止被多敌人同时攻击秒杀
            if (this.IsInvincible)
            {
                return;
            }

            // 记录本次受击时间，启动无敌帧冷却
            this.lastDamageTime = Time.time;

            base.ReduceHp(hp, attacker, isCRT);
            if (NetworkConnect.Instance.IsOnline && !this.pv.IsMine && PhotonNetwork.IsConnected)
            {
                return;
            }

            PlayerData playerData = this.CharacterDataLAB as PlayerData;
            PlayerStatusUI.Instance.UpdatePlayerState(
                this.CharacterDataLAB.Hp,
                this.CharacterDataLAB.MaxHp,
                playerData.Mp,
                playerData.MaxMp,
                playerData.Level,
                playerData.CurExperience,
                playerData.MaxExperience);
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
                LogManager.Instance.Log("pos is null!!!", LogManager.LogLevelEnum.Error);
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
            GameplaySessionStats.Instance.RecordPlayerDeath();
            LogManager.Instance.Log("玩家死亡", LogManager.LogLevelEnum.Info);
            this.CharacterDataLAB.Hp = 1; // Keep alive at 1 HP to prevent re-death during respawn

            // Hide from enemies by switching off the Player layer temporarily
            this.gameObject.layer = LayerMask.NameToLayer("Default");

            DeathPenaltyManager.Instance.HandlePlayerDeath(this);

            // 自动采集会话结算数据（F011：补齐结算系统缺失的自动触发链路）
            SessionResultAutoTrigger.NotifyPlayerDeath();
        }

        /// <summary>
        /// 玩家移动.
        /// </summary>
        private void Move()
        {
            if (Input.GetKey(KeyCode.A) ||
                Input.GetKey(KeyCode.W) ||
                Input.GetKey(KeyCode.S) ||
                Input.GetKey(KeyCode.D) ||
                (Joystick.Instance && Joystick.Instance.Direction.sqrMagnitude > 0.02f))
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

                this.animator.SetBool("IsMove", true);

                // 按键控制玩家
                this.direction.x = Input.GetAxisRaw("Horizontal"); // 在Game面板
                this.direction.y = Input.GetAxisRaw("Vertical");

                // 摇杆控制玩家
                if (this.direction.x == 0 && this.direction.y == 0 && Joystick.Instance != null)
                {
                    this.direction.x = Joystick.Instance.Direction.x;
                    this.direction.y = Joystick.Instance.Direction.y;
                }

                if (this.direction.y > 0)
                {
                    // 上
                    this.animator.SetInteger("Direction", 0);
                }
                else if (this.direction.y < 0)
                {
                    // 下
                    this.animator.SetInteger("Direction", 1);
                }
                else if (this.direction.x < 0)
                {
                    // 左
                    this.animator.SetInteger("Direction", 2);
                }
                else if (this.direction.x > 0)
                {
                    // 右
                    this.animator.SetInteger("Direction", 3);
                }

                this.rg.velocity = this.MoveSpeed * this.direction.normalized;
            }
            else
            {
                this.animator.SetBool("IsMove", false);
                this.rg.velocity = Vector3.zero;
            }
        }

        private void OnDestroy()
        {
            PlayerManager.Instance.Remove(this);

            // 关闭游戏添加正在装备的武器
            PlayerData playerData = this.CharacterDataLAB as PlayerData;
            if (playerData.Weapon != null)
            {
                BackpackController.Instance.AddItem(playerData.Weapon);
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