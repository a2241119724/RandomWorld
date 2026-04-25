namespace LAB2D
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 新游戏或者继续游戏面板
    /// </summary>
    public class NewOrContinuePanel : ABasePanel<NewOrContinuePanel>
    {
        private const int SlotColumnCount = 2;
        private static readonly Vector2 SlotSize = new (280.0f, 72.0f);
        private static readonly Vector2 SlotSpacing = new (16.0f, 12.0f);
        private readonly List<Button> archiveSlotButtons = new ();
        private Button newGameButton;
        private Button continueGameButton;
        private Transform archiveSlotRoot;

        public NewOrContinuePanel()
        {
            this.Name = "NewOrContinue";
            this.Init();
            this.newGameButton = Tool.GetComponentInChildren<Button>(this.Panel, "NewGame");
            this.continueGameButton = Tool.GetComponentInChildren<Button>(this.Panel, "ContinueGame");
            this.archiveSlotRoot = Tool.GetComponentInChildren<Transform>(this.Panel, "Content");
            this.CreateArchiveSlotButtons();
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            this.RefreshArchiveSlotButtons();
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
        }

        private void CreateArchiveSlotButtons()
        {
            if (this.newGameButton == null || this.continueGameButton == null || this.archiveSlotRoot == null)
            {
                this.BindDefaultButtons();
                return;
            }

            RectTransform rootRect = this.archiveSlotRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.18f, 0.12f);
            rootRect.anchorMax = new Vector2(0.82f, 0.82f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = Vector2.zero;
            rootRect.pivot = new Vector2(0.5f, 1.0f);

            HorizontalLayoutGroup horizontalLayout = this.archiveSlotRoot.GetComponent<HorizontalLayoutGroup>();
            if (horizontalLayout != null)
            {
                horizontalLayout.enabled = false;
            }

            this.newGameButton.gameObject.SetActive(false);
            this.continueGameButton.gameObject.SetActive(false);

            for (int i = 0; i < ArchiveManager.Instance.ArchiveCount; i++)
            {
                Button archiveSlotButton = UnityEngine.Object.Instantiate(
                    this.continueGameButton.gameObject,
                    this.archiveSlotRoot,
                    false).GetComponent<Button>();
                int archiveIndex = i;
                archiveSlotButton.name = $"ArchiveSlot_{archiveIndex + 1}";
                archiveSlotButton.onClick.RemoveAllListeners();
                archiveSlotButton.onClick.AddListener(() => this.OnClick_ArchiveSlot(archiveIndex));
                this.SetArchiveSlotButtonPosition(archiveSlotButton, archiveIndex);
                archiveSlotButton.gameObject.SetActive(true);
                this.archiveSlotButtons.Add(archiveSlotButton);
            }
        }

        private void SetArchiveSlotButtonPosition(Button archiveSlotButton, int archiveIndex)
        {
            RectTransform rectTransform = archiveSlotButton.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                return;
            }

            int row = archiveIndex / SlotColumnCount;
            int column = archiveIndex % SlotColumnCount;
            float x = (column - ((SlotColumnCount - 1) * 0.5f)) * (SlotSize.x + SlotSpacing.x);
            float y = -row * (SlotSize.y + SlotSpacing.y);
            rectTransform.anchorMin = new Vector2(0.5f, 1.0f);
            rectTransform.anchorMax = new Vector2(0.5f, 1.0f);
            rectTransform.pivot = new Vector2(0.5f, 1.0f);
            rectTransform.anchoredPosition = new Vector2(x, y);
            rectTransform.sizeDelta = SlotSize;
            rectTransform.localScale = Vector3.one;
        }

        private void BindDefaultButtons()
        {
            if (this.newGameButton != null)
            {
                this.newGameButton.onClick.AddListener(() =>
                {
                    ArchiveManager.Instance.SetCurrentArchive(0);
                    this.StartNewArchive();
                });
            }

            if (this.continueGameButton != null)
            {
                this.continueGameButton.onClick.AddListener(() =>
                {
                    ArchiveManager.Instance.SetCurrentArchive(0);
                    if (!ArchiveManager.Instance.HasCurrentArchive())
                    {
                        GlobalInit.Instance.ShowTip("没有存档!!!");
                        return;
                    }

                    this.LoadArchive();
                });
            }
        }

        private void RefreshArchiveSlotButtons()
        {
            for (int i = 0; i < this.archiveSlotButtons.Count; i++)
            {
                bool hasArchive = ArchiveManager.Instance.HasArchive(i);
                Text text = this.archiveSlotButtons[i].GetComponentInChildren<Text>(true);
                if (text != null)
                {
                    text.text = hasArchive ? $"存档 {i + 1}\n继续游戏" : $"存档 {i + 1}\n新游戏";
                    text.resizeTextForBestFit = true;
                    text.resizeTextMinSize = 16;
                    text.resizeTextMaxSize = 34;
                }
            }
        }

        private void OnClick_ArchiveSlot(int archiveIndex)
        {
            ArchiveManager.Instance.SetCurrentArchive(archiveIndex);
            if (ArchiveManager.Instance.HasCurrentArchive())
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
            ArchiveManager.Instance.LoadCurrentArchive();
        }
    }
}
