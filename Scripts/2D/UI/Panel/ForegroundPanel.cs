namespace LAB2D.UI.Panel
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Core;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Player;
    using System.Collections.Generic;
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    /// <summary>
    /// 游戏前景面板
    /// </summary>
    public class ForegroundPanel : ABasePanel<ForegroundPanel>
    {
        private readonly List<Button> saveSlotButtons = new ();
        private GameObject saveSlotPanel;
        private GameObject overwriteConfirmPanel;
        private Text overwriteConfirmText;
        private RectTransform saveSlotContent;
        private int pendingOverwriteArchiveIndex = -1;
        private GameObject renamePanel;
        private InputField renameInputField;
        private Text renameTipText;
        private int pendingRenameArchiveIndex = -1;
        private GameObject clearConfirmPanel;
        private Text clearConfirmText;
        private int pendingClearArchiveIndex = -1;

        public ForegroundPanel()
        {
            this.Name = "Foreground";
            this.Init();
            LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.Panel, "Pause").onClick.AddListener(this.OnClick_Pause);
            Button attack = LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.Panel, "Attack");
            if (attack != null)
            {
                LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.Panel, "Attack").onClick.AddListener(this.Onclick_Attack);
            }

            LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.Panel, "Setting").onClick.AddListener(this.Onclick_Setting);

            ServiceLocator.Get<EventBus>().Subscribe<PlayerAttackRequestedEvent>(this.OnPlayerAttackRequested);
            Button save = LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.Panel, "Save");
            if (save == null)
            {
                AWorkerTask.LogProvider("ForegroundPanel: Save button not found in Panel hierarchy!", LogManager.LogLevelEnum.Error);
            }
            else if (AWorkerTask.NetworkIsOnlineProvider() && !AWorkerTask.NetworkIsMasterClientProvider())
            {
                // 仅在联机房间中且非房主时隐藏保存按钮
                save.gameObject.SetActive(false);
            }
            else
            {
                save.onClick.AddListener(this.Onclick_Save);
            }

            this.CreateSaveSlotPanel();
            this.CreateClearConfirmPanel();
            this.CreateRenamePanel();
        }

        /// <summary>
        /// 游戏速率
        /// </summary>
        public float TimeScale { get; set; } = 3;

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
            this.ExecuteAttack();
        }

        private void OnPlayerAttackRequested(PlayerAttackRequestedEvent e)
        {
            this.ExecuteAttack();
        }

        private void ExecuteAttack()
        {
            if (ServiceLocator.Get<PlayerManager>().Mine.Weapon != null)
            {
                AWeaponObject weapon = ServiceLocator.Get<PlayerManager>().Mine.Weapon.GetComponent<AWeaponObject>();
                weapon.IsCRT = UnityEngine.Random.Range(0.0f, 1.0f) < ServiceLocator.Get<PlayerManager>().Mine.CharacterDataLAB.CRT;
                if (ServiceLocator.Get<NetworkConnect>().IsOnline)
                {
                    ServiceLocator.Get<PlayerManager>().Mine.Weapon.GetComponent<PhotonView>().RPC("Attack", RpcTarget.All);
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

        private void Onclick_Save()
        {
            this.ShowSaveSlotPanel();
        }

        private void CreateSaveSlotPanel()
        {
            this.BindSceneSaveSlotPanel();
        }

        private bool BindSceneSaveSlotPanel()
        {
            Transform panelTransform = this.FindChildTransform(this.Panel.transform, "SaveSlotPanel");
            if (panelTransform == null)
            {
                return false;
            }

            this.saveSlotPanel = panelTransform.gameObject;

            Transform viewportTransform = this.FindChildTransform(panelTransform, "SaveSlotViewport");
            if (viewportTransform == null)
            {
                return false;
            }

            Transform contentTransform = this.FindChildTransform(viewportTransform, "SaveSlotContent");
            this.saveSlotContent = contentTransform.GetComponent<RectTransform>();
            viewportTransform.GetComponent<ScrollRect>().content = this.saveSlotContent;

            this.ClearGeneratedSaveSlotButtons();
            this.CreateSaveSlotButtons();

            Button closeButton = this.FindChildComponent<Button>(panelTransform, "Close");
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(this.HideSaveSlotPanel);

            this.BindSceneOverwriteConfirmPanel(panelTransform);
            this.saveSlotPanel.SetActive(false);
            return true;
        }

        private void ClearGeneratedSaveSlotButtons()
        {
            for (int i = this.saveSlotContent.childCount - 1; i >= 0; i--)
            {
                Transform child = this.saveSlotContent.GetChild(i);
                if (!child.name.StartsWith("SaveSlot_", System.StringComparison.Ordinal))
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(child.gameObject);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }

            this.saveSlotButtons.Clear();
        }

        private void CreateSaveSlotButtons()
        {
            this.saveSlotButtons.Clear();
            for (int i = 0; i < ServiceLocator.Get<ArchiveManager>().ArchiveCount; i++)
            {
                int archiveIndex = i;
                Button button = this.CreateSaveSlotButton(archiveIndex);
                this.saveSlotButtons.Add(button);
            }
        }

        private Button CreateSaveSlotButton(int archiveIndex)
        {
            GameObject gameObject = ServiceLocator.Get<ResourceManager>().Instantiate(PrefabConstant.ARCHIVE_ITEM, this.saveSlotContent, false);
            gameObject.name = $"SaveSlot_{archiveIndex + 1}";
            gameObject.layer = this.saveSlotContent.gameObject.layer;
            Button button = this.FindChildComponent<Button>(gameObject.transform, "Save");
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => this.OnClick_SaveSlot(archiveIndex));

            Button renameButton = this.FindChildComponent<Button>(gameObject.transform, "Rename");
            renameButton.onClick.RemoveAllListeners();
            renameButton.onClick.AddListener(() => this.ShowRenamePanel(archiveIndex));

            Button clearButton = this.FindChildComponent<Button>(gameObject.transform, "Clear");
            clearButton.onClick.RemoveAllListeners();
            clearButton.onClick.AddListener(() => this.ShowClearConfirmPanel(archiveIndex));

            return button;
        }

        private void BindSceneOverwriteConfirmPanel(Transform panelTransform)
        {
            Transform confirmPanelTransform = this.FindChildTransform(panelTransform, "OverwriteConfirm");
            this.overwriteConfirmPanel = confirmPanelTransform.gameObject;
            this.overwriteConfirmText = this.FindChildComponent<Text>(confirmPanelTransform, "Tip");

            Button confirmButton = this.FindChildComponent<Button>(confirmPanelTransform, "Confirm");
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(this.OnClick_ConfirmOverwrite);

            Button cancelButton = this.FindChildComponent<Button>(confirmPanelTransform, "Cancel");
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(this.HideOverwriteConfirmPanel);

            this.overwriteConfirmPanel.SetActive(false);
        }

        private Transform FindChildTransform(Transform root, string name)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in transforms)
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }

        private T FindChildComponent<T>(Transform root, string name)
            where T : Component
        {
            Transform child = this.FindChildTransform(root, name);
            return child == null ? null : child.GetComponent<T>();
        }

        private void ShowSaveSlotPanel()
        {
            if (this.saveSlotPanel == null)
            {
                AWorkerTask.LogProvider("ForegroundPanel: SaveSlotPanel not found in scene — save UI unavailable.", LogManager.LogLevelEnum.Error);
                ServiceLocator.Get<GlobalInit>().ShowTip("存档面板未配置，请联系开发者");
                return;
            }

            this.RefreshSaveSlotButtons();
            this.saveSlotPanel.SetActive(true);
            this.saveSlotPanel.transform.SetAsLastSibling();
        }

        private void HideSaveSlotPanel()
        {
            this.pendingOverwriteArchiveIndex = -1;
            this.HideOverwriteConfirmPanel();
            this.HideClearConfirmPanel();
            this.HideRenamePanel();
            this.saveSlotPanel.SetActive(false);
        }

        private void RefreshSaveSlotButtons()
        {
            for (int i = 0; i < this.saveSlotButtons.Count; i++)
            {
                bool hasArchive = ServiceLocator.Get<ArchiveManager>().HasArchive(i);
                Text text = this.FindChildComponent<Text>(this.saveSlotButtons[i].transform, "Text");
                string displayName = ServiceLocator.Get<ArchiveManager>().GetArchiveDisplayName(i);
                string status = hasArchive ? "已有存档" : "空槽";
                text.text = $"{displayName}\n{status}";
            }
        }

        private void OnClick_SaveSlot(int archiveIndex)
        {
            if (ServiceLocator.Get<ArchiveManager>().HasArchive(archiveIndex))
            {
                this.ShowOverwriteConfirmPanel(archiveIndex);
                return;
            }

            this.SaveToArchive(archiveIndex);
        }

        private void ShowOverwriteConfirmPanel(int archiveIndex)
        {
            this.pendingOverwriteArchiveIndex = archiveIndex;
            string displayName = ServiceLocator.Get<ArchiveManager>().GetArchiveDisplayName(archiveIndex);
            this.overwriteConfirmText.text = $"{displayName} 已存在\n是否确认覆盖?";
            this.overwriteConfirmPanel.SetActive(true);
            this.overwriteConfirmPanel.transform.SetAsLastSibling();
        }

        private void HideOverwriteConfirmPanel()
        {
            this.overwriteConfirmPanel.SetActive(false);
        }

        private void OnClick_ConfirmOverwrite()
        {
            if (this.pendingOverwriteArchiveIndex < 0)
            {
                this.HideOverwriteConfirmPanel();
                return;
            }

            this.SaveToArchive(this.pendingOverwriteArchiveIndex);
        }

        private void CreateClearConfirmPanel()
        {
            if (this.saveSlotPanel == null)
            {
                return;
            }

            Transform panelTransform = this.FindChildTransform(this.saveSlotPanel.transform, "ClearConfirm");
            if (panelTransform == null)
            {
                return;
            }

            this.clearConfirmPanel = panelTransform.gameObject;
            this.clearConfirmText = this.FindChildComponent<Text>(panelTransform, "Tip");

            Button confirmButton = this.FindChildComponent<Button>(panelTransform, "Confirm");
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(this.OnClick_ConfirmClear);

            Button cancelButton = this.FindChildComponent<Button>(panelTransform, "Cancel");
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(this.HideClearConfirmPanel);

            this.clearConfirmPanel.SetActive(false);
        }

        private void ShowClearConfirmPanel(int archiveIndex)
        {
            if (this.clearConfirmPanel == null)
            {
                return;
            }

            if (!ServiceLocator.Get<ArchiveManager>().HasArchive(archiveIndex))
            {
                ServiceLocator.Get<GlobalInit>().ShowTip("空存档槽不能清除");
                this.RefreshSaveSlotButtons();
                return;
            }

            this.pendingClearArchiveIndex = archiveIndex;
            string displayName = ServiceLocator.Get<ArchiveManager>().GetArchiveDisplayName(archiveIndex);
            this.clearConfirmText.text = $"确认清除存档\n{displayName}?";
            this.clearConfirmPanel.SetActive(true);
            this.clearConfirmPanel.transform.SetAsLastSibling();
        }

        private void HideClearConfirmPanel()
        {
            this.pendingClearArchiveIndex = -1;
            if (this.clearConfirmPanel != null)
            {
                this.clearConfirmPanel.SetActive(false);
            }
        }

        private void OnClick_ConfirmClear()
        {
            if (this.pendingClearArchiveIndex < 0)
            {
                this.HideClearConfirmPanel();
                return;
            }

            if (ServiceLocator.Get<ArchiveManager>().DeleteArchive(this.pendingClearArchiveIndex))
            {
                ServiceLocator.Get<GlobalInit>().ShowTip("存档已清除");
            }
            else
            {
                ServiceLocator.Get<GlobalInit>().ShowTip("清除存档失败");
            }

            this.HideClearConfirmPanel();
            this.RefreshSaveSlotButtons();
        }

        private void CreateRenamePanel()
        {
            if (this.saveSlotPanel == null)
            {
                return;
            }

            Transform panelTransform = this.FindChildTransform(this.saveSlotPanel.transform, "RenameArchive");
            if (panelTransform == null)
            {
                return;
            }

            this.renamePanel = panelTransform.gameObject;
            this.renameInputField = this.FindChildComponent<InputField>(panelTransform, "NameInput");
            this.renameTipText = this.FindChildComponent<Text>(panelTransform, "Tip");

            Button confirmButton = this.FindChildComponent<Button>(panelTransform, "Confirm");
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(this.OnClick_ConfirmRename);

            Button cancelButton = this.FindChildComponent<Button>(panelTransform, "Cancel");
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(this.HideRenamePanel);

            this.renamePanel.SetActive(false);
        }

        private void ShowRenamePanel(int archiveIndex)
        {
            if (this.renamePanel == null)
            {
                return;
            }

            if (!ServiceLocator.Get<ArchiveManager>().HasArchive(archiveIndex))
            {
                ServiceLocator.Get<GlobalInit>().ShowTip("空存档槽不能改名");
                this.RefreshSaveSlotButtons();
                return;
            }

            string displayName = ServiceLocator.Get<ArchiveManager>().GetArchiveDisplayName(archiveIndex);
            this.pendingRenameArchiveIndex = archiveIndex;
            if (this.renameTipText != null)
            {
                this.renameTipText.text = $"修改存档名称\n{displayName}";
            }

            this.renamePanel.SetActive(true);
            this.renamePanel.transform.SetAsLastSibling();

            if (this.renameInputField != null)
            {
                this.renameInputField.text = displayName;
                Canvas.ForceUpdateCanvases();
                this.renameInputField.Select();
                this.renameInputField.ActivateInputField();
                EventSystem.current.SetSelectedGameObject(this.renameInputField.gameObject);
            }
        }

        private void HideRenamePanel()
        {
            this.pendingRenameArchiveIndex = -1;
            if (this.renamePanel != null)
            {
                this.renamePanel.SetActive(false);
            }
        }

        private void OnClick_ConfirmRename()
        {
            if (this.pendingRenameArchiveIndex < 0)
            {
                this.HideRenamePanel();
                return;
            }

            string displayName = this.renameInputField == null ? string.Empty : this.renameInputField.text;
            if (!ServiceLocator.Get<ArchiveManager>().SetArchiveDisplayName(this.pendingRenameArchiveIndex, displayName))
            {
                ServiceLocator.Get<GlobalInit>().ShowTip("存档名称不能为空");
                return;
            }

            ServiceLocator.Get<GlobalInit>().ShowTip("存档名称已修改");
            this.HideRenamePanel();
            this.RefreshSaveSlotButtons();
        }

        private void SaveToArchive(int archiveIndex)
        {
            ServiceLocator.Get<ArchiveManager>().SetCurrentArchive(archiveIndex);
            ServiceLocator.Get<GlobalInit>().ShowTip($"保存数据: {ServiceLocator.Get<ArchiveManager>().CurrentArchiveDisplayName}");
            ServiceLocator.Get<ArchiveManager>().SaveCurrentArchive();
            this.HideSaveSlotPanel();
        }
    }
}
