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
        private RectTransform content;
        private Font uiFont;
        private Sprite buttonSprite;

        public NewOrContinuePanel()
        {
            this.Name = "NewOrContinue";
            this.Init();
            this.content = Tool.GetComponentInChildren<RectTransform>(this.Panel, "Content");
            this.uiFont = this.GetUIFont();
            this.buttonSprite = Resources.Load<Sprite>("Images/UI/ButtonBackground");
            this.BindArchiveSlotButtons();
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

            for (int i = 0; i < ArchiveManager.Instance.ArchiveCount; i++)
            {
                int archiveIndex = i;
                Button archiveSlotButton = this.FindArchiveSlotButton(archiveIndex) ??
                    this.CreateArchiveSlotButton(archiveIndex);
                archiveSlotButton.onClick.RemoveAllListeners();
                archiveSlotButton.onClick.AddListener(() => this.OnClick_ArchiveSlot(archiveIndex));
                this.SetArchiveSlotButtonPosition(archiveSlotButton, archiveIndex);
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

                return child.GetComponent<Button>();
            }

            return null;
        }

        private Button CreateArchiveSlotButton(int archiveIndex)
        {
            GameObject gameObject = this.CreateUIObject($"ArchiveSlot_{archiveIndex + 1}", this.content);
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = SlotSize;

            Image image = gameObject.AddComponent<Image>();
            image.sprite = this.buttonSprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;

            Button button = gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1.0f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1.0f);
            colors.selectedColor = new Color(0.96f, 0.96f, 0.96f, 1.0f);
            colors.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f);
            button.colors = colors;
            button.targetGraphic = image;

            GameObject textObject = this.CreateText("Title", gameObject.transform);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            return button;
        }

        private GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject gameObject = new (name);
            gameObject.transform.SetParent(parent, false);
            gameObject.layer = parent.gameObject.layer;
            gameObject.AddComponent<RectTransform>();
            return gameObject;
        }

        private GameObject CreateText(string name, Transform parent)
        {
            GameObject gameObject = this.CreateUIObject(name, parent);
            Text text = gameObject.AddComponent<Text>();
            text.font = this.uiFont;
            text.fontSize = 24;
            text.fontStyle = FontStyle.Bold;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 16;
            text.resizeTextMaxSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.9843137f, 0.39215687f, 0.0f, 1.0f);
            text.raycastTarget = false;
            return gameObject;
        }

        private void SetArchiveSlotButtonPosition(Button archiveSlotButton, int archiveIndex)
        {
            RectTransform rectTransform = archiveSlotButton.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                return;
            }

            float y = -archiveIndex * (SlotSize.y + SlotSpacing);
            rectTransform.anchorMin = new Vector2(0.0f, 1.0f);
            rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
            rectTransform.pivot = new Vector2(0.5f, 1.0f);
            rectTransform.anchoredPosition = new Vector2(0.0f, y);
            rectTransform.sizeDelta = new Vector2(0.0f, SlotSize.y);
            rectTransform.offsetMin = new Vector2(0.0f, rectTransform.offsetMin.y);
            rectTransform.offsetMax = new Vector2(0.0f, rectTransform.offsetMax.y);
            rectTransform.localScale = Vector3.one;
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

        private Font GetUIFont()
        {
            Text[] texts = this.Panel.GetComponentsInChildren<Text>(true);
            foreach (Text text in texts)
            {
                if (text.font != null)
                {
                    return text.font;
                }
            }

            Font font = Resources.Load<Font>("Font/ark-pixel-12px-monospaced-zh_cn");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
