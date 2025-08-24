namespace LAB2D
{
    using System;
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
        private SpriteRenderer spriteRendererIdle; // idle图像开关
        private CameraMove mainCamera;
        private CameraMove miniCamera;

        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();
            this.direction = default;
            if (this.direction == null)
            {
                LogManager.Instance.Log("direction assign resource Error!!!", LogManager.LogLevel.Error);
                return;
            }

            this.spriteRendererIdle = this.gameObject.GetComponent<SpriteRenderer>();
            this.name = "Player";
            this.CharacterDataLAB = new PlayerData();
        }

        /// <inheritdoc/>
        public override void Start()
        {
            base.Start();
            this.animator = this.GetComponent<Animator>();
            if (this.animator == null)
            {
                LogManager.Instance.Log("animator Not Found!!!", LogManager.LogLevel.Error);
                return;
            }

            // 不在线，或者在线并且是自己
            if (this.pv.IsMine || !NetworkConnect.Instance.IsOnline)
            {
                this.MoveSpeed = 10;
                if (GameInfoUI.Instance != null)
                {
                    GameInfoUI.Instance.SetPosition(this.transform.position);
                }

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

        public void FixedUpdate()
        {
            // 如果观察的当期的角色并且连接服务器,防止误操作别的玩家
            if (NetworkConnect.Instance.IsOnline && !this.pv.IsMine && PhotonNetwork.IsConnected)
            {
                return;
            }

            // 防止撞墙震动
            this.Move();
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

        /// <summary>
        /// 减血.
        /// </summary>
        /// <param name="hp">血量.</param>
        public override void ReduceHp(float hp)
        {
            if (hp <= 0)
            {
                LogManager.Instance.Log("Hp can't less than zero!!!", LogManager.LogLevel.Error);
                return;
            }

            base.ReduceHp(hp);
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
                LogManager.Instance.Log("pos is null!!!", LogManager.LogLevel.Error);
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
            LogManager.Instance.Log("玩家重生", LogManager.LogLevel.Info);
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
                this.mainCamera.DirectToPosition(this.transform.position);
                this.miniCamera.DirectToPosition(this.transform.position);
                this.miniCamera.Character = this;

                if (GameInfoUI.Instance != null)
                {
                    GameInfoUI.Instance.SetPosition(this.transform.position);
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

                this.transform.Translate(this.MoveSpeed * Time.deltaTime * this.direction.normalized, Space.World);

                // 翻转
                this.spriteRendererIdle.flipX = this.direction.x < 0;
            }
            else
            {
                this.animator.SetBool("IsMove", false);
            }
        }

        private void OnDestroy()
        {
            PlayerManager.Instance.Remove(this);

            // 关闭游戏添加正在装备的武器
            if (PlayerManager.Instance.Select.Id != -1)
            {
                BackpackController.Instance.AddItem(PlayerManager.Instance.Select.WeaponData);
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