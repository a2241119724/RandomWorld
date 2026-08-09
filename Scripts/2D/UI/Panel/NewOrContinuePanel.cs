namespace LAB2D.UI.Panel
{
    using LAB2D;
    using LAB2D.Core;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 新游戏或者继续游戏面板
    /// </summary>
    public class NewOrContinuePanel : ABasePanel<NewOrContinuePanel>
    {
        private readonly List<Button> archiveSlotButtons = new ();
        private RectTransform content;
        private GameObject renamePanel;
        private InputField renameInputField;
        private Text renameTipText;
        private int pendingRenameArchiveIndex = -1;
        private GameObject clearConfirmPanel;
        private Text clearConfirmText;
        private int pendingClearArchiveIndex = -1;

        public NewOrContinuePanel()
        {
            this.Name = "NewOrContinuePanel";
            this.Init();
            this.content = LAB2D.Tool.Tool.GetComponentInChildren<RectTransform>(this.Panel, "Content");
            this.BindArchiveSlotButtons();
            this.CreateRenamePanel();
            this.CreateClearConfirmPanel();
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            this.RefreshArchiveSlotButtons();
        }

        private void BindArchiveSlotButtons()
        {
            if (this.content == null)
            {
                return;
            }

            this.archiveSlotButtons.Clear();

            for (int i = 0; i < ServiceLocator.Get<ArchiveManager>().ArchiveCount; i++)
            {
                int archiveIndex = i;
                Button archiveSlotButton = this.FindArchiveSlotButton(archiveIndex) ??
                    this.CreateArchiveSlotButton(archiveIndex);
                archiveSlotButton.onClick.RemoveAllListeners();
                archiveSlotButton.onClick.AddListener(() => this.OnClick_ArchiveSlot(archiveIndex));
                Transform root = archiveSlotButton.transform.parent;
                Button renameButton = this.FindChildComponent<Button>(root, "Rename");
                renameButton.onClick.RemoveAllListeners();
                renameButton.onClick.AddListener(() => this.ShowRenamePanel(archiveIndex));

                Button clearButton = this.FindChildComponent<Button>(root, "Clear");
                clearButton.onClick.RemoveAllListeners();
                clearButton.onClick.AddListener(() => this.ShowClearConfirmPanel(archiveIndex));

                archiveSlotButton.gameObject.SetActive(true);
                this.archiveSlotButtons.Add(archiveSlotButton);
            }
        }

        private Button FindArchiveSlotButton(int archiveIndex)
        {
            string slotName = $"ArchiveSlot_{archiveIndex + 1}";
            for (int i = 0; i < this.content.childCount; i++)
            {
                Transform child = this.content.GetChild(i);
                if (child.name != slotName)
                {
                    continue;
                }

                return this.FindChildComponent<Button>(child, "Save");
            }

            return null;
        }

        private Button CreateArchiveSlotButton(int archiveIndex)
        {
            GameObject gameObject = ServiceLocator.Get<ResourceManager>().Instantiate(PrefabConstant.ARCHIVE_ITEM, this.content, false);
            gameObject.name = $"ArchiveSlot_{archiveIndex + 1}";
            gameObject.layer = this.content.gameObject.layer;
            return this.FindChildComponent<Button>(gameObject.transform, "Save");
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

        private void RefreshArchiveSlotButtons()
        {
            for (int i = 0; i < this.archiveSlotButtons.Count; i++)
            {
                bool hasArchive = ServiceLocator.Get<ArchiveManager>().HasArchive(i);
                Text text = this.FindChildComponent<Text>(this.archiveSlotButtons[i].transform, "Text");
                string displayName = ServiceLocator.Get<ArchiveManager>().GetArchiveDisplayName(i);
                string status = hasArchive ? "继续游戏" : "新游戏";
                text.text = $"{displayName}\n{status}";
            }
        }

        private void CreateRenamePanel()
        {
            Transform panelTransform = this.FindChildTransform(this.Panel.transform, "RenameArchive");
            if (panelTransform != null)
            {
                this.renamePanel = panelTransform.gameObject;
                this.renameInputField = this.FindChildComponent<InputField>(panelTransform, "NameInput");
                this.renameTipText = this.FindChildComponent<Text>(panelTransform, "Tip");

                Button confirmButton = this.FindChildComponent<Button>(panelTransform, "Confirm");
                if (confirmButton != null)
                {
                    confirmButton.onClick.RemoveAllListeners();
                    confirmButton.onClick.AddListener(this.OnClick_ConfirmRename);
                }

                Button cancelButton = this.FindChildComponent<Button>(panelTransform, "Cancel");
                if (cancelButton != null)
                {
                    cancelButton.onClick.RemoveAllListeners();
                    cancelButton.onClick.AddListener(this.HideRenamePanel);
                }

                this.renamePanel.SetActive(false);
                return;
            }
        }

        private void ShowRenamePanel(int archiveIndex)
        {
            if (!ServiceLocator.Get<ArchiveManager>().HasArchive(archiveIndex))
            {
                ServiceLocator.Get<GlobalInit>().ShowTip("空存档槽不能改名");
                this.RefreshArchiveSlotButtons();
                return;
            }

            string displayName = ServiceLocator.Get<ArchiveManager>().GetArchiveDisplayName(archiveIndex);
            this.pendingRenameArchiveIndex = archiveIndex;
            if (this.renameTipText != null)
            {
                this.renameTipText.text = $"修改存档名称\n{displayName}";
            }

            if (this.renameInputField != null)
            {
                this.renameInputField.text = displayName;
            }

            if (this.renamePanel != null)
            {
                this.renamePanel.SetActive(true);
                this.renamePanel.transform.SetAsLastSibling();
            }

            if (this.renameInputField != null)
            {
                this.renameInputField.Select();
                this.renameInputField.ActivateInputField();
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
            this.RefreshArchiveSlotButtons();
        }

        private void CreateClearConfirmPanel()
        {
            Transform panelTransform = this.FindChildTransform(this.Panel.transform, "ClearConfirm");
            if (panelTransform != null)
            {
                this.clearConfirmPanel = panelTransform.gameObject;
                this.clearConfirmText = this.FindChildComponent<Text>(panelTransform, "Tip");

                Button confirmButton = this.FindChildComponent<Button>(panelTransform, "Confirm");
                if (confirmButton != null)
                {
                    confirmButton.onClick.RemoveAllListeners();
                    confirmButton.onClick.AddListener(this.OnClick_ConfirmClear);
                }

                Button cancelButton = this.FindChildComponent<Button>(panelTransform, "Cancel");
                if (cancelButton != null)
                {
                    cancelButton.onClick.RemoveAllListeners();
                    cancelButton.onClick.AddListener(this.HideClearConfirmPanel);
                }

                this.clearConfirmPanel.SetActive(false);
                return;
            }
        }

        private void ShowClearConfirmPanel(int archiveIndex)
        {
            if (!ServiceLocator.Get<ArchiveManager>().HasArchive(archiveIndex))
            {
                ServiceLocator.Get<GlobalInit>().ShowTip("空存档槽不能清除");
                this.RefreshArchiveSlotButtons();
                return;
            }

            this.pendingClearArchiveIndex = archiveIndex;
            string displayName = ServiceLocator.Get<ArchiveManager>().GetArchiveDisplayName(archiveIndex);
            if (this.clearConfirmText != null)
            {
                this.clearConfirmText.text = $"确认清除存档\n{displayName}?";
            }

            if (this.clearConfirmPanel != null)
            {
                this.clearConfirmPanel.SetActive(true);
                this.clearConfirmPanel.transform.SetAsLastSibling();
            }
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
            this.RefreshArchiveSlotButtons();
        }

        private void OnClick_ArchiveSlot(int archiveIndex)
        {
            ServiceLocator.Get<ArchiveManager>().SetCurrentArchive(archiveIndex);
            if (ServiceLocator.Get<ArchiveManager>().HasCurrentArchive())
            {
                this.LoadArchive();
                return;
            }

            this.StartNewArchive();
        }

        private void StartNewArchive()
        {
            this.Controller.Close();
            GlobalData.IsNew = true;
            this.Controller.Show(CreateDataPanel.Instance);
        }

        private void LoadArchive()
        {
            this.Controller.Close();
            GlobalData.IsNew = false;
            this.Controller.Show(AsyncProgressPanel.Instance);
            ServiceLocator.Get<ArchiveManager>().LoadCurrentArchive();
        }
    }
}
