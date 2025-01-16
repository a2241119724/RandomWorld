using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace LAB2D {
    public class ForegroundPanel : BasePanel<ForegroundPanel>
    {
        public float TimeScale { get; set; } = 1;

        public ForegroundPanel()
        {
            Name = "Foreground";
            setPanel();
            Tool.GetComponentInChildren<Button>(panel, "Backpack").onClick.AddListener(OnClick_Backpack);
            Tool.GetComponentInChildren<Button>(panel, "Pause").onClick.AddListener(OnClick_Pause);
            Button attack = Tool.GetComponentInChildren<Button>(panel, "Attack");
            if (attack != null)
            {
                Tool.GetComponentInChildren<Button>(panel, "Attack").onClick.AddListener(Onclick_Attack);
            }
            Tool.GetComponentInChildren<Button>(panel, "PlayerInfo").onClick.AddListener(Onclick_PlayerInfo);
            Tool.GetComponentInChildren<Button>(panel, "Setting").onClick.AddListener(Onclick_Setting);
            Tool.GetComponentInChildren<Button>(panel, "GeneratorWorker").onClick.AddListener(Onclick_GeneratorWorker);
            Tool.GetComponentInChildren<Button>(panel, "Build").onClick.AddListener(Onclick_Build);
            Tool.GetComponentInChildren<Button>(panel, "WorkerInfo").onClick.AddListener(Onclick_WorkerInfo);
            Tool.GetComponentInChildren<Button>(panel, "Save").onClick.AddListener(Onclick_Save);
        }

        public override void OnEnter()
        {
            base.OnEnter();
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override void OnPause()
        {
            base.OnPause();
            // 射线是否能穿透(使得不能点击按钮)
            panel.GetComponent<CanvasGroup>().blocksRaycasts = false;
            Time.timeScale = 0; // 暂停
        }

        public override void OnRun()
        {
            base.OnRun();
            // 射线是否能穿透(使得能点击按钮)
            panel.GetComponent<CanvasGroup>().blocksRaycasts = true;
            Time.timeScale = TimeScale; // 暂停
        }

        /// <summary>
        /// 打开背包
        /// </summary>
        private void OnClick_Backpack()
        {
            // 创建背包面板
            controller.show(BackpackMenuPanel.Instance);
        }

        /// <summary>
        /// 暂停游戏
        /// </summary>
        private void OnClick_Pause()
        {
            controller.show(PauseMenuPanel.Instance);
        }

        /// <summary>
        /// 玩家攻击
        /// </summary>
        public void Onclick_Attack()
        {
            if (PlayerManager.Instance.Select.weapon != null)
            {
                //PlayerManager.Instance.Select.weapon.GetComponent<Weapon>().attack();
                PlayerManager.Instance.Select.weapon.GetComponent<PhotonView>().RPC("Attack", RpcTarget.All);
            }
        }

        /// <summary>
        /// 打开玩家面板
        /// </summary>
        public void Onclick_PlayerInfo()
        {
            controller.show(PlayerInfoPanel.Instance);
        }

        /// <summary>
        /// 打开设置面板
        /// </summary>
        public void Onclick_Setting()
        {
            controller.show(SettingMenuPanel.Instance);
        }

        /// <summary>
        /// 测试生成玩家
        /// </summary>
        public void Onclick_GeneratorWorker()
        {
            WorkerManager.Instance.create(PlayerManager.Instance.Mine.transform.position);
        }

        public void Onclick_Build()
        {
            controller.show(BuildMenuPanel.Instance);
        }

        public void Onclick_WorkerInfo()
        {
            controller.show(WorkerInfoPanel.Instance);
        }
        public void Onclick_Save()
        {
            GlobalInit.Instance.showTip("保存数据");
            foreach (ASaveData saveData in ASaveData.Instances)
            {
                saveData.saveData();
            }
            foreach (AMonoSaveData saveData in AMonoSaveData.Instances)
            {
                saveData.saveData();
            }
        }
    }
}