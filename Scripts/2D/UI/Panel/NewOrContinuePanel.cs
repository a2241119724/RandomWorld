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
        private const float SlotSpacing = 12.0f;
        private static readonly Vector2 SlotSize = new (560.0f, 72.0f);
        private readonly List<Button> archiveSlotButtons = new ();
        private Button newGameButton;
        private Button continueGameButton;
        private RectTransform archiveSlotContent;
        private Transform archiveSlotViewport;

        public NewOrContinuePanel()
        {
            this.Name = "NewOrContinue";
            this.Init();
            this.newGameButton = Tool.GetComponentInChildren<Button>(this.Panel, "NewGame");
            this.continueGameButton = Tool.GetComponentInChildren<Button>(this.Panel, "ContinueGame");
            this.archiveSlotViewport = Tool.GetComponentInChildren<Transform>(this.Panel, "Content");
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
            if (this.newGameButton == null || this.continueGameButton == null || this.archiveSlotViewport == null)
            {
                this.BindDefaultButtons();
                return;
            }

            this.ConfigureArchiveSlotViewport();

            this.archiveSlotContent = this.CreateArchiveSlotContent();

            this.newGameButton.gameObject.SetActive(false);
            this.continueGameButton.gameObject.SetActive(false);

            for (int i = 0; i < ArchiveManager.Instance.ArchiveCount; i++)
            {
                Button archiveSlotButton = UnityEngine.Object.Instantiate(
                    this.continueGameButton.gameObject,
                    this.archiveSlotContent,
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

        private void ConfigureArchiveSlotViewport()
        {
            HorizontalLayoutGroup horizontalLayout = this.archiveSlotViewport.GetComponent<HorizontalLayoutGroup>();
            if (horizontalLayout != null)
            {
                horizontalLayout.enabled = false;
            }

            Image image = this.archiveSlotViewport.GetComponent<Image>();
            if (image == null)
            {
                image = this.archiveSlotViewport.gameObject.AddComponent<Image>();
                image.color = new Color(0.0f, 0.0f, 0.0f, 0.25f);
            }

            image.raycastTarget = true;

            RectMask2D mask = this.archiveSlotViewport.GetComponent<RectMask2D>();
            if (mask == null)
            {
                this.archiveSlotViewport.gameObject.AddComponent<RectMask2D>();
            }
        }

        private RectTransform CreateArchiveSlotContent()
        {
            Transform contentTransform = this.FindChildTransform(this.archiveSlotViewport, "ArchiveSlotContent");
            if (contentTransform == null)
            {
                GameObject content = new ("ArchiveSlotContent");
                content.transform.SetParent(this.archiveSlotViewport, false);
                content.layer = this.archiveSlotViewport.gameObject.layer;
                contentTransform = content.transform;
            }

            RectTransform contentRect = contentTransform.GetComponent<RectTransform>();
            if (contentRect == null)
            {
                contentRect = contentTransform.gameObject.AddComponent<RectTransform>();
            }

            contentRect.anchorMin = new Vector2(0.5f, 1.0f);
            contentRect.anchorMax = new Vector2(0.5f, 1.0f);
            contentRect.pivot = new Vector2(0.5f, 1.0f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = this.GetSlotContentSize();
            this.ClearGeneratedArchiveSlotButtons(contentRect);

            ScrollRect scrollRect = this.archiveSlotViewport.GetComponent<ScrollRect>();
            if (scrollRect == null)
            {
                scrollRect = this.archiveSlotViewport.gameObject.AddComponent<ScrollRect>();
            }

            scrollRect.content = contentRect;
            scrollRect.viewport = this.archiveSlotViewport.GetComponent<RectTransform>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 50.0f;
            return contentRect;
        }

        private void ClearGeneratedArchiveSlotButtons(RectTransform contentRect)
        {
            for (int i = contentRect.childCount - 1; i >= 0; i--)
            {
                Transform child = contentRect.GetChild(i);
                if (!child.name.StartsWith("ArchiveSlot_", System.StringComparison.Ordinal))
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

            this.archiveSlotButtons.Clear();
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

        private Vector2 GetSlotContentSize()
        {
            int count = ArchiveManager.Instance.ArchiveCount;
            float height = (count * SlotSize.y) + (Mathf.Max(0, count - 1) * SlotSpacing);
            return new Vector2(SlotSize.x, height);
        }

        private void SetArchiveSlotButtonPosition(Button archiveSlotButton, int archiveIndex)
        {
            RectTransform rectTransform = archiveSlotButton.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                return;
            }

            float y = -archiveIndex * (SlotSize.y + SlotSpacing);
            rectTransform.anchorMin = new Vector2(0.5f, 1.0f);
            rectTransform.anchorMax = new Vector2(0.5f, 1.0f);
            rectTransform.pivot = new Vector2(0.5f, 1.0f);
            rectTransform.anchoredPosition = new Vector2(0.0f, y);
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
