using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace LAB2D {
    public class ForegroundPanel : BasePanel<ForegroundPanel>
    {
        public float TimeScale { get; set; } = 1;
        public UnityAction[] ToolMenus { get; set; }

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
            Tool.GetComponentInChildren<Button>(panel, "WorkerTask").onClick.AddListener(Onclick_WorkerTaskInfo);
            Tool.GetComponentInChildren<Button>(panel, "Save").onClick.AddListener(Onclick_Save);
            Tool.GetComponentInChildren<Button>(panel, "Inventory").onClick.AddListener(Onclick_Inventory);
            Tool.GetComponentInChildren<Button>(panel, "Chat").onClick.AddListener(Onclick_Chat);
            // ƥ�����ְ���
            ToolMenus = new UnityAction[9];
            ToolMenus[0] += Onclick_GeneratorWorker;
            ToolMenus[1] += Onclick_Build;
            ToolMenus[2] += Onclick_PlayerInfo;
            ToolMenus[3] += OnClick_Backpack;
            ToolMenus[4] += Onclick_WorkerTaskInfo;
            ToolMenus[5] += Onclick_Inventory;
            ToolMenus[6] += Onclick_Chat;
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
            // �����Ƿ��ܴ�͸(ʹ�ò��ܵ����ť)
            panel.GetComponent<CanvasGroup>().blocksRaycasts = false;
            Time.timeScale = 0; // ��ͣ
        }

        public override void OnRun()
        {
            base.OnRun();
            // �����Ƿ��ܴ�͸(ʹ���ܵ����ť)
            panel.GetComponent<CanvasGroup>().blocksRaycasts = true;
            Time.timeScale = TimeScale; // ��ͣ
        }

        /// <summary>
        /// �򿪱���
        /// </summary>
        private void OnClick_Backpack()
        {
            // �����������
            controller.show(BackpackMenuPanel.Instance);
        }

        /// <summary>
        /// ��ͣ��Ϸ
        /// </summary>
        private void OnClick_Pause()
        {
            controller.show(PauseMenuPanel.Instance);
        }

        /// <summary>
        /// ��ҹ���
        /// </summary>
        public void Onclick_Attack()
        {
            if (PlayerManager.Instance.Select.weapon != null)
            {
                if (NetworkConnect.Instance.IsOnline)
                {
                    PlayerManager.Instance.Select.weapon.GetComponent<PhotonView>().RPC("Attack", RpcTarget.All);
                }
                else
                {
                    PlayerManager.Instance.Select.weapon.GetComponent<WeaponObject>().attack();
                }
            }
        }

        /// <summary>
        /// ��������
        /// </summary>
        public void Onclick_PlayerInfo()
        {
            controller.show(PlayerInfoPanel.Instance);
        }

        /// <summary>
        /// ���������
        /// </summary>
        public void Onclick_Setting()
        {
            controller.show(SettingMenuPanel.Instance);
        }

        /// <summary>
        /// �����������
        /// </summary>
        public void Onclick_GeneratorWorker()
        {
            WorkerManager.Instance.create(PlayerManager.Instance.Mine.transform.position);
        }

        public void Onclick_Build()
        {
            controller.show(BuildMenuPanel.Instance);
        }

        public void Onclick_WorkerTaskInfo()
        {
            controller.show(WorkerTaskInfoPanel.Instance);
        }
        public void Onclick_Save()
        {
            GlobalInit.Instance.showTip("��������");
            foreach (ASaveData saveData in ASaveData.Instances)
            {
                saveData.saveData();
            }
            foreach (AMonoSaveData saveData in AMonoSaveData.Instances)
            {
                saveData.saveData();
            }
        }

        public void Onclick_Inventory()
        {
            controller.show(InventoryMenuPanel.Instance);
        }

        public void Onclick_Chat()
        {
            controller.show(AIChatPanel.Instance);
        }
    }
}