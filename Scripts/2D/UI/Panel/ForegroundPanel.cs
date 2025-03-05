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
        /// <summary>
        /// ƥ�����ְ���
        /// </summary>
        public readonly IBasePanel[] ToolMenus = new IBasePanel[] { BuildMenuPanel.Instance, PlayerInfoPanel.Instance, BackpackMenuPanel.Instance,
                WorkerTaskInfoPanel.Instance,InventoryMenuPanel.Instance,AIChatPanel.Instance};

    public ForegroundPanel()
        {
            Name = "Foreground";
            setPanel();
            Tool.GetComponentInChildren<Button>(panel, "Pause").onClick.AddListener(OnClick_Pause);
            Button attack = Tool.GetComponentInChildren<Button>(panel, "Attack");
            if (attack != null)
            {
                Tool.GetComponentInChildren<Button>(panel, "Attack").onClick.AddListener(Onclick_Attack);
            }
            Tool.GetComponentInChildren<Button>(panel, "Setting").onClick.AddListener(Onclick_Setting);
            Tool.GetComponentInChildren<Button>(panel, "GeneratorWorker").onClick.AddListener(Onclick_GeneratorWorker);
            Tool.GetComponentInChildren<Button>(panel, "GeneratorItem").onClick.AddListener(Onclick_GeneratorItem);
            Tool.GetComponentInChildren<Button>(panel, "Save").onClick.AddListener(Onclick_Save);
            // ƥ�����ְ���
            Transform tools = Tool.GetComponentInChildren<Transform>(panel, "Panel");
            for (int i = 0; i < tools.childCount; i++)
            {
                int temp = i;
                tools.GetChild(i).GetComponent<Button>().onClick.AddListener(() =>
                {
                    controller.show(ToolMenus[temp]);
                });
            }
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

        public void Onclick_GeneratorItem()
        {
            if(EnemyManager.Instance.Characters.Count > 0)
            {
                ((EnemyDeadState)EnemyManager.Instance.Characters[0].Manager.States[EnemyStateType.Dead]).dropItem();
            }
        }
    }
}