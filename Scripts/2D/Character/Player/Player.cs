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
        /// 当前装备的武器物体
        /// </summary>
        public GameObject Weapon { get; set; }

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
            this.AttackLayers = LayerMask.GetMask("Tile", LayerConstant.ENEMY_LAAYER);
            this.AttackTags = new List<string>
            {
                "Enemy",
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
            this.Attack();
        }

        public void FixedUpdate()
        {
            // 如果观察的当期的角色并且连接服务器,防止误操作别的玩家
            if (NetworkConnect.Instance.IsOnline && !this.pv.IsMine && PhotonNetwork.IsConnected)
            {
                return;
            }

            this.Move(); // 防止撞墙震动
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

        /// <summary>
        /// 增加经验值.
        /// </summary>
        /// <param name="experience">经验值.</param>
        public void AddExperienceValue(int experience)
        {
            PlayerData playerData = this.CharacterDataLAB as PlayerData;
            playerData.CurExperience += experience;

            // 升级
            if (playerData.CurExperience / playerData.MaxExperience >= 1)
            {
                ++playerData.Level;
                playerData.CurExperience %= playerData.MaxExperience;
                playerData.MaxExperience *= 2;
                GlobalInit.Instance.ShowTip("UP " + playerData.Level);
                this.CharacterDataLAB.ComputeAttribute();
            }

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
        public override void ReduceHp(float hp, bool isCRT = false)
        {
            if (hp <= 0)
            {
                LogManager.Instance.Log("Hp can't less than zero!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            base.ReduceHp(hp, isCRT);
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
            LogManager.Instance.Log("玩家重生", LogManager.LogLevelEnum.Info);
            this.CharacterDataLAB.Hp = 100;
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
            /// <summary>
            /// 玩家蓝量
            /// </summary>
            public int Mp = 100;

            /// <summary>
            /// 玩家最大蓝量
            /// </summary>
            public int MaxMp = 100;

            /// <summary>
            /// 玩家当前经验值
            /// </summary>
            public int CurExperience = 0;

            /// <summary>
            /// 玩家当前等级最大经验值
            /// </summary>
            public int MaxExperience = 4;

            /// <summary>
            /// 当前等级
            /// </summary>
            public int Level = 1;
        }
    }
}