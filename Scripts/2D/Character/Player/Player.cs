using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

namespace LAB2D
{
    // 渲染更新 Update,不受Time.timeScale影响;Time.deltaTime,受Time.timeScale影响
    // 物理更新 FixedUpdate,受Time.timeScale影响
    public class Player : Character
    {
        [Tooltip("角色速度")]
        private int Mp = 100; // 玩家蓝量
        private int maxMp = 100; // 玩家最大蓝量
        private int currentExperience = 0; // 玩家当前经验值
        private int maxExperience = 4; // 玩家当前等级最大经验值
        private int level = 1; // 当前等级
        private Animator animator;
        private Vector3 direction; // 电脑按键方向
        private SpriteRenderer spriteRenderer; // idle图像开关
        private CameraMove mainCamera;
        private CameraMove miniCamera;

        protected override void Awake()
        {
            base.Awake();
            direction = new Vector3();
            if(direction == null)
            {
                Debug.LogError("direction assign resource Error!!!");
                return;
            }
            CharacterDataLAB.MaxHp = CharacterDataLAB.Hp = 100;
            spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
            name = "Player";
        }

        protected override void Start()
        {
            base.Start();
            animator = GetComponent<Animator>();
            if (animator == null) {
                Debug.LogError("animator Not Found!!!");
                return;
            }
            if (PlayerPositionUI.Instance != null)
            {
                PlayerPositionUI.Instance.setPosition(transform.position);
            }
            //if (Map.Instance != null)
            //{
            //    Map.Instance.show(transform.position);
            //}
            if (photonView.IsMine)
            {
                mainCamera = Camera.main.GetComponent<CameraMove>();
                miniCamera = GameObject.FindGameObjectWithTag(ResourceConstant.MINIMAP_TAG).GetComponent<CameraMove>();
                mainCamera.directToPosition(transform.position);
                miniCamera.directToPosition(transform.position);
                PlayerManager.Instance.Mine = this;
                PhotonNetwork.LocalPlayer.TagObject = this;
                Tool.GetComponentInChildren<Text>(gameObject, "Name").text = PhotonNetwork.NickName;
                PlayerStatusUI.Instance.updatePlayerState(CharacterDataLAB.Hp, CharacterDataLAB.MaxHp, Mp, maxMp, level, currentExperience, maxExperience);
            }
            else {
                Tool.GetComponentInChildren<Text>(gameObject, "Name").text = photonView.Owner.NickName;
                PlayerManager.Instance.add(this);
                //PhotonNetwork.PlayerList[PhotonNetwork.PlayerList.Length - 1].TagObject = this;
            }
        }

        private void FixedUpdate()
        {
            // 如果观察的当期的角色并且连接服务器,防止误操作别的玩家
            if (!photonView.IsMine && PhotonNetwork.IsConnected) return;
            move();
        }

        /// <summary>
        /// 玩家移动
        /// </summary>
        private void move()
        {
            if (Input.GetKey(KeyCode.A) ||
                Input.GetKey(KeyCode.W) ||
                Input.GetKey(KeyCode.S) ||
                Input.GetKey(KeyCode.D) ||
                (Joystick.Instance && Joystick.Instance.Direction.sqrMagnitude > 0.02f)){
                mainCamera.directToPosition(transform.position);
                miniCamera.directToPosition(transform.position);
                if (PlayerPositionUI.Instance != null)
                {
                    PlayerPositionUI.Instance.setPosition(transform.position);
                }
                //if (Map.Instance != null)
                //{
                //    Map.Instance.show(transform.position);
                //}
                animator.SetBool("IsMove", true);
                // 按键控制玩家
                direction.x = Input.GetAxisRaw("Horizontal"); // 在Game面板
                direction.y = Input.GetAxisRaw("Vertical");
                // 摇杆控制玩家
                if(direction.x == 0 && direction.y == 0 &&Joystick.Instance != null) {
                    direction.x = Joystick.Instance.Direction.x;
                    direction.y = Joystick.Instance.Direction.y;
                }
                transform.Translate(direction.normalized * Time.deltaTime * moveSpeed, Space.World);
                // 翻转
                _renderer.flipX = direction.x < 0;
                spriteRenderer.enabled = false;
                for (int i=0;i<7;i++) {
                    transform.GetChild(i).gameObject.SetActive(true);
                }
            }
            else
            {
                animator.SetBool("IsMove", false);
                spriteRenderer.enabled = true;
                for (int i = 0; i < 7; i++)
                {
                    transform.GetChild(i).gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 增加经验值
        /// </summary>
        /// <param name="experience"></param>
        public void addExperienceValue(int experience)
        {
            currentExperience += experience;
            // 升级
            if (currentExperience / maxExperience >= 1)
            {
                ++level;
                currentExperience %= maxExperience;
                maxExperience *= 2;
                GlobalInit.Instance.showTip("UP " + level);
            }
            PlayerStatusUI.Instance.updatePlayerState(CharacterDataLAB.Hp, CharacterDataLAB.MaxHp, Mp, maxMp, level, currentExperience, maxExperience);
        }

        /// <summary>
        /// 加血
        /// </summary>
        /// <param name="Hp">加血</param>
        public void addHp(float Hp)
        {
            CharacterDataLAB.Hp += Hp;
            if (CharacterDataLAB.Hp > CharacterDataLAB.MaxHp) {
                CharacterDataLAB.Hp = CharacterDataLAB.MaxHp;
            }
            PlayerStatusUI.Instance.updatePlayerState(CharacterDataLAB.Hp, CharacterDataLAB.MaxHp, Mp, maxMp, level, currentExperience, maxExperience);
        }

        /// <summary>
        /// 减血
        /// </summary>
        /// <param name="Hp">减的血量</param>
        public override void reduceHp(float Hp) {
            if(Hp <= 0)
            {
                Debug.LogError("Hp can't less than zero!!!");
                return;
            }
            base.reduceHp(Hp);
            if (!photonView.IsMine && PhotonNetwork.IsConnected) return;
            PlayerStatusUI.Instance.updatePlayerState(CharacterDataLAB.Hp, CharacterDataLAB.MaxHp, Mp, maxMp, level, currentExperience, maxExperience);
        }

        private void OnDestroy()
        {
            PlayerManager.Instance.remove(this);
            // 关闭游戏添加正在装备的武器
            if (PlayerManager.Instance.Select.currentId != -1) { 
                BackpackController.Instance.addItem(PlayerManager.Instance.Select.weaponData);
            }
        }

        protected override void death()
        {
            Debug.Log("玩家重生");
            CharacterDataLAB.Hp = 100;
        }

        /// <summary>
        /// 是否在玩家周围
        /// </summary>
        /// <returns></returns>
        public bool isArround(Vector3 pos) {
            if (pos == null) {
                Debug.LogError("pos is null!!!");
                return false;
            }
            return pos.x < transform.position.x + 50 &&
                pos.x > transform.position.x - 50 &&
                pos.y > transform.position.y - 50 &&
                pos.y < transform.position.y + 50;
        }

        /// <summary>
        /// 切换角色视角
        /// </summary>
        /// <param name="is_2_5D"></param>
        public void togglePerspective(bool is_2_5D) {
            float rotationX = 0;
            if (is_2_5D){
                rotationX = -45;
            }
            transform.rotation = Quaternion.Euler(rotationX, transform.rotation.y, transform.rotation.z);
            Camera.main.transform.rotation = Quaternion.Euler(new Vector3(rotationX, 0, 0));
            //
            if (is_2_5D)
            {
                Camera.main.orthographic = false;
                Camera.main.fieldOfView = 100;
                mainCamera.Offset = new Vector3(0, -6, 14);
            }
            else
            {
                Camera.main.orthographic = true;
                Camera.main.orthographicSize = 10;
                mainCamera.Offset = new Vector3(0, 0, 0);
            }
        }

        /// <summary>
        /// 都有碰撞器,其中之一勾选Is Trigger,其中之一带有刚体
        /// </summary>
        /// <param name="collider"></param>
        //private void OnTriggerEnter2D(Collider2D collider)
        //{
        //    if (collider.gameObject.CompareTag("Enemy"))
        //    {  
        //    }
        //}

        /// <summary>
        /// 都有碰撞器,其中之一带有刚体,都不勾选Is Trigger
        /// </summary>
        //private void OnColisionEnter2D(Collision2D collision) {
        //    //collision.contacts[0].point; // 碰撞的第一个点
        //    //collision.contacts[0].normal; // 碰撞的法线
        //}
    }
}