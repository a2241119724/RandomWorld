namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 游戏前景面板
    /// </summary>
    public class ForegroundPanel : ABasePanel<ForegroundPanel>
    {
        /// <summary>
        /// 匹配数字按键
        /// </summary>
        public readonly IBasePanel[] ToolMenus = new IBasePanel[]
        {
            BuildMenuPanel.Instance, BackpackMenuPanel.Instance,
            WorkerTaskTogglePanel.Instance, InventoryMenuPanel.Instance, AIChatPanel.Instance,
        };

        public ForegroundPanel()
        {
            this.Name = "Foreground";
            this.Init();
            Tool.GetComponentInChildren<Button>(this.Panel, "Pause").onClick.AddListener(this.OnClick_Pause);
            Button attack = Tool.GetComponentInChildren<Button>(this.Panel, "Attack");
            if (attack != null)
            {
                Tool.GetComponentInChildren<Button>(this.Panel, "Attack").onClick.AddListener(this.Onclick_Attack);
            }

            Tool.GetComponentInChildren<Button>(this.Panel, "Setting").onClick.AddListener(this.Onclick_Setting);
            Tool.GetComponentInChildren<Button>(this.Panel, "GeneratorWorker").onClick.AddListener(this.Onclick_GeneratorWorker);
            Tool.GetComponentInChildren<Button>(this.Panel, "GeneratorItem").onClick.AddListener(this.Onclick_GeneratorItem);
            Button save = Tool.GetComponentInChildren<Button>(this.Panel, "Save");
            if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
            {
                save.gameObject.SetActive(false);
            }
            else
            {
                save.onClick.AddListener(this.Onclick_Save);
            }

            // 匹配数字按键
            Transform tools = Tool.GetComponentInChildren<Transform>(this.Panel, "Menu");
            for (int i = 0; i < tools.childCount; i++)
            {
                int temp = i;
                tools.GetChild(i).GetComponent<Button>().onClick.AddListener(() =>
                {
                    this.Controller.Show(this.ToolMenus[temp]);
                });
            }
        }

        /// <summary>
        /// 游戏速率
        /// </summary>
        public float TimeScale { get; set; } = 1;

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
        }

        /// <inheritdoc/>
        public override void OnPause()
        {
            base.OnPause();

            // 射线是否能穿透(使得不能点击按钮)
            this.Panel.GetComponent<CanvasGroup>().blocksRaycasts = false;
            Time.timeScale = 0; // 暂停
        }

        /// <inheritdoc/>
        public override void OnRun()
        {
            base.OnRun();

            // 射线是否能穿透(使得能点击按钮)
            this.Panel.GetComponent<CanvasGroup>().blocksRaycasts = true;
            Time.timeScale = this.TimeScale; // 暂停
        }

        /// <summary>
        /// 玩家攻击
        /// </summary>
        public void Onclick_Attack()
        {
            if (PlayerManager.Instance.Mine.Weapon != null)
            {
                AWeaponObject weapon = PlayerManager.Instance.Mine.Weapon.GetComponent<AWeaponObject>();
                weapon.IsCRT = UnityEngine.Random.Range(0.0f, 1.0f) < PlayerManager.Instance.Mine.CharacterDataLAB.CRT;
                if (NetworkConnect.Instance.IsOnline)
                {
                    PlayerManager.Instance.Mine.Weapon.GetComponent<PhotonView>().RPC("Attack", RpcTarget.All);
                }
                else
                {
                    weapon.Attack();
                }
            }
        }

        /// <summary>
        /// 暂停游戏
        /// </summary>
        private void OnClick_Pause()
        {
            this.Controller.Show(PauseMenuPanel.Instance);
        }

        /// <summary>
        /// 打开设置面板
        /// </summary>
        private void Onclick_Setting()
        {
            this.Controller.Show(SettingMenuPanel.Instance);
        }

        /// <summary>
        /// 测试生成玩家
        /// </summary>
        private void Onclick_GeneratorWorker()
        {
            WorkerManager.Instance.Create(PlayerManager.Instance.Mine.transform.position);
        }

        private void Onclick_Save()
        {
            GlobalInit.Instance.ShowTip("保存数据");
            List<Type> types = Tool.GetChildByParent<ASaveData>();
            foreach (Type type in types)
            {
                PropertyInfo propertyInfo = Tool.GetStaticPropertyByType(type, "Instance");
                if (propertyInfo == null)
                {
                    continue;
                }

                // 实例化
                object obj = propertyInfo.GetValue(null, null);
                Tool.GetMethodByType(type, "SaveData")?.Invoke(obj, null);
            }

            types = Tool.GetChildByParent<AMonoSaveData>();
            foreach (Type type in types)
            {
                PropertyInfo propertyInfo = Tool.GetStaticPropertyByType(type, "Instance");
                if (propertyInfo == null)
                {
                    continue;
                }

                // 实例化
                object obj = propertyInfo.GetValue(null, null);
                Tool.GetMethodByType(type, "SaveData")?.Invoke(obj, null);
            }
        }

        private void Onclick_GeneratorItem()
        {
            if (EnemyManager.Instance.Characters.Count > 0)
            {
                ((CommonEnemyDeadState)((ACommonEnemy)EnemyManager.Instance.Characters[0]).Manager.States[ACommonEnemyState.TypeEnum.Dead]).DropItem();
            }
        }
    }
}