namespace LAB2D
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
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
        private GameObject renamePanel;
        private InputField renameInputField;
        private Text renameTipText;
        private int pendingRenameArchiveIndex = -1;
        private GameObject clearConfirmPanel;
        private Text clearConfirmText;
        private int pendingClearArchiveIndex = -1;

        public NewOrContinuePanel()
        {
            this.Name = "NewOrContinue";
            this.Init();
            this.content = Tool.GetComponentInChildren<RectTransform>(this.Panel, "Content");
            this.uiFont = this.GetUIFont();
            this.buttonSprite = Resources.Load<Sprite>("Images/UI/ButtonBackground");
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

            for (int i = 0; i < ArchiveManager.Instance.ArchiveCount; i++)
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

                return this.FindChildComponent<Button>(child, "Save");
            }

            return null;
        }

        private Button CreateArchiveSlotButton(int archiveIndex)
        {
            GameObject gameObject = ResourceManager.Instance.Instantiate(PrefabConstant.ARCHIVE_ITEM, this.content, false);
            gameObject.name = $"ArchiveSlot_{archiveIndex + 1}";
            gameObject.layer = this.content.gameObject.layer;
            return this.FindChildComponent<Button>(gameObject.transform, "Save");
        }

        private GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject gameObject = new (name);
            gameObject.transform.SetParent(parent, false);
            gameObject.layer = parent.gameObject.layer;
            gameObject.AddComponent<RectTransform>();
            return gameObject;
        }

        private Button CreateButton(string name, Transform parent, string text, Vector2 size, UnityAction onClick)
        {
            GameObject gameObject = this.CreateUIObject(name, parent);
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = size;

            Image image = gameObject.AddComponent<Image>();
            image.sprite = this.buttonSprite;
            image.type = Image.Type.Simple;
            image.color = PixelUITheme.ButtonNormal;

            Button button = gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = PixelUITheme.ButtonNormal;
            colors.highlightedColor = PixelUITheme.ButtonHighlighted;
            colors.pressedColor = PixelUITheme.ButtonPressed;
            colors.selectedColor = PixelUITheme.ButtonSelected;
            colors.disabledColor = new Color(0.95f, 0.63f, 0.69f, 0.4f);
            button.colors = colors;
            button.targetGraphic = image;
            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            GameObject textObject = this.CreateText(
                "Text",
                gameObject.transform,
                text,
                22,
                TextAnchor.MiddleCenter,
                Color.white,
                FontStyle.Bold);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            return button;
        }

        private GameObject CreateText(string name, Transform parent)
        {
            return this.CreateText(
                name,
                parent,
                string.Empty,
                24,
                TextAnchor.MiddleCenter,
                PixelUITheme.SaveSlotTitleText,
                FontStyle.Bold);
        }

        private GameObject CreateText(
            string name,
            Transform parent,
            string textValue,
            int fontSize,
            TextAnchor alignment,
            Color color,
            FontStyle fontStyle = FontStyle.Normal)
        {
            GameObject gameObject = this.CreateUIObject(name, parent);
            Text text = gameObject.AddComponent<Text>();
            text.font = this.uiFont;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 16;
            text.resizeTextMaxSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.text = textValue;
            text.raycastTarget = false;
            return gameObject;
        }

        private void SetArchiveSlotButtonPosition(Button archiveSlotButton, int archiveIndex)
        {
            RectTransform rectTransform = archiveSlotButton.transform.parent.GetComponent<RectTransform>();
            float y = -archiveIndex * (SlotSize.y + SlotSpacing);
            rectTransform.anchoredPosition = new Vector2(0.0f, y);
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
                bool hasArchive = ArchiveManager.Instance.HasArchive(i);
                Transform root = this.archiveSlotButtons[i].transform.parent;
                Text text = this.FindChildComponent<Text>(root, "Title");
                string displayName = ArchiveManager.Instance.GetArchiveDisplayName(i);
                string status = hasArchive ? "继续游戏" : "新游戏";
                text.text = $"{displayName}\n{status}";

                Button renameButton = this.FindChildComponent<Button>(root, "Rename");
                renameButton.gameObject.SetActive(hasArchive);

                Button clearButton = this.FindChildComponent<Button>(root, "Clear");
                clearButton.gameObject.SetActive(hasArchive);
            }
        }

        private void CreateRenamePanel()
        {
            Transform panelTransform = this.FindChildTransform(this.Panel.transform, "RenameArchivePanel");
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

            this.CreateRuntimeRenamePanel();
        }

        private void CreateRuntimeRenamePanel()
        {
            this.renamePanel = this.CreateUIObject("RenameArchivePanel", this.Panel.transform);
            RectTransform panelRect = this.renamePanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image shade = this.renamePanel.AddComponent<Image>();
            shade.color = PixelUITheme.DialogShadeDark;
            shade.raycastTarget = true;

            GameObject box = this.CreateUIObject("Box", this.renamePanel.transform);
            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.anchoredPosition = Vector2.zero;
            boxRect.sizeDelta = new Vector2(560.0f, 300.0f);

            Image boxImage = box.AddComponent<Image>();
            boxImage.color = PixelUITheme.DialogBoxBg;
            boxImage.raycastTarget = true;

            GameObject tip = this.CreateText(
                "Tip",
                box.transform,
                "修改存档名称",
                32,
                TextAnchor.MiddleCenter,
                PixelUITheme.TextPrimary,
                FontStyle.Bold);
            RectTransform tipRect = tip.GetComponent<RectTransform>();
            tipRect.anchorMin = new Vector2(0.5f, 0.5f);
            tipRect.anchorMax = new Vector2(0.5f, 0.5f);
            tipRect.pivot = new Vector2(0.5f, 0.5f);
            tipRect.anchoredPosition = new Vector2(0.0f, 95.0f);
            tipRect.sizeDelta = new Vector2(500.0f, 60.0f);
            this.renameTipText = tip.GetComponent<Text>();

            this.renameInputField = this.CreateNameInputField(box.transform);

            Button confirmButton = this.CreateButton(
                "Confirm",
                box.transform,
                "确认",
                new Vector2(180.0f, 58.0f),
                this.OnClick_ConfirmRename);
            RectTransform confirmRect = confirmButton.GetComponent<RectTransform>();
            confirmRect.anchorMin = new Vector2(0.5f, 0.5f);
            confirmRect.anchorMax = new Vector2(0.5f, 0.5f);
            confirmRect.pivot = new Vector2(0.5f, 0.5f);
            confirmRect.anchoredPosition = new Vector2(-105.0f, -95.0f);

            Button cancelButton = this.CreateButton(
                "Cancel",
                box.transform,
                "取消",
                new Vector2(180.0f, 58.0f),
                this.HideRenamePanel);
            RectTransform cancelRect = cancelButton.GetComponent<RectTransform>();
            cancelRect.anchorMin = new Vector2(0.5f, 0.5f);
            cancelRect.anchorMax = new Vector2(0.5f, 0.5f);
            cancelRect.pivot = new Vector2(0.5f, 0.5f);
            cancelRect.anchoredPosition = new Vector2(105.0f, -95.0f);

            this.renamePanel.SetActive(false);
        }

        private InputField CreateNameInputField(Transform parent)
        {
            GameObject inputObject = this.CreateUIObject("NameInput", parent);
            RectTransform inputRect = inputObject.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0.5f, 0.5f);
            inputRect.anchorMax = new Vector2(0.5f, 0.5f);
            inputRect.pivot = new Vector2(0.5f, 0.5f);
            inputRect.anchoredPosition = new Vector2(0.0f, 22.0f);
            inputRect.sizeDelta = new Vector2(440.0f, 58.0f);

            Image inputBackground = inputObject.AddComponent<Image>();
            inputBackground.color = Color.white;
            inputBackground.raycastTarget = true;

            InputField inputField = inputObject.AddComponent<InputField>();
            inputField.characterLimit = ArchiveManager.ArchiveDisplayNameMaxLength;
            inputField.lineType = InputField.LineType.SingleLine;

            Text text = this.CreateInputFieldText(
                "Text",
                inputObject.transform,
                string.Empty,
                Color.black,
                26);
            text.supportRichText = false;
            inputField.textComponent = text;

            Text placeholder = this.CreateInputFieldText(
                "Placeholder",
                inputObject.transform,
                "请输入存档名称",
                new Color(0.55f, 0.55f, 0.55f, 1.0f),
                24);
            inputField.placeholder = placeholder;
            return inputField;
        }

        private Text CreateInputFieldText(string name, Transform parent, string textValue, Color color, int fontSize)
        {
            GameObject textObject = this.CreateText(
                name,
                parent,
                textValue,
                fontSize,
                TextAnchor.MiddleLeft,
                color);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12.0f, 0.0f);
            textRect.offsetMax = new Vector2(-12.0f, 0.0f);
            return textObject.GetComponent<Text>();
        }

        private void ShowRenamePanel(int archiveIndex)
        {
            if (!ArchiveManager.Instance.HasArchive(archiveIndex))
            {
                GlobalInit.Instance.ShowTip("空存档槽不能改名");
                this.RefreshArchiveSlotButtons();
                return;
            }

            string displayName = ArchiveManager.Instance.GetArchiveDisplayName(archiveIndex);
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
            if (!ArchiveManager.Instance.SetArchiveDisplayName(this.pendingRenameArchiveIndex, displayName))
            {
                GlobalInit.Instance.ShowTip("存档名称不能为空");
                return;
            }

            GlobalInit.Instance.ShowTip("存档名称已修改");
            this.HideRenamePanel();
            this.RefreshArchiveSlotButtons();
        }

        private void CreateClearConfirmPanel()
        {
            Transform panelTransform = this.FindChildTransform(this.Panel.transform, "ClearConfirmPanel");
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

            this.CreateRuntimeClearConfirmPanel();
        }

        private void CreateRuntimeClearConfirmPanel()
        {
            this.clearConfirmPanel = this.CreateUIObject("ClearConfirmPanel", this.Panel.transform);
            RectTransform panelRect = this.clearConfirmPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image shade = this.clearConfirmPanel.AddComponent<Image>();
            shade.color = PixelUITheme.DialogShadeDark;
            shade.raycastTarget = true;

            GameObject box = this.CreateUIObject("Box", this.clearConfirmPanel.transform);
            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.anchoredPosition = Vector2.zero;
            boxRect.sizeDelta = new Vector2(560.0f, 260.0f);

            Image boxImage = box.AddComponent<Image>();
            boxImage.color = PixelUITheme.DialogBoxBg;
            boxImage.raycastTarget = true;

            GameObject tip = this.CreateText(
                "Tip",
                box.transform,
                string.Empty,
                32,
                TextAnchor.MiddleCenter,
                PixelUITheme.TextPrimary,
                FontStyle.Bold);
            RectTransform tipRect = tip.GetComponent<RectTransform>();
            tipRect.anchorMin = new Vector2(0.5f, 0.5f);
            tipRect.anchorMax = new Vector2(0.5f, 0.5f);
            tipRect.pivot = new Vector2(0.5f, 0.5f);
            tipRect.anchoredPosition = new Vector2(0.0f, 55.0f);
            tipRect.sizeDelta = new Vector2(500.0f, 110.0f);
            this.clearConfirmText = tip.GetComponent<Text>();

            Button confirmButton = this.CreateButton(
                "Confirm",
                box.transform,
                "确认清除",
                new Vector2(190.0f, 58.0f),
                this.OnClick_ConfirmClear);
            RectTransform confirmRect = confirmButton.GetComponent<RectTransform>();
            confirmRect.anchorMin = new Vector2(0.5f, 0.5f);
            confirmRect.anchorMax = new Vector2(0.5f, 0.5f);
            confirmRect.pivot = new Vector2(0.5f, 0.5f);
            confirmRect.anchoredPosition = new Vector2(-110.0f, -70.0f);

            Button cancelButton = this.CreateButton(
                "Cancel",
                box.transform,
                "取消",
                new Vector2(190.0f, 58.0f),
                this.HideClearConfirmPanel);
            RectTransform cancelRect = cancelButton.GetComponent<RectTransform>();
            cancelRect.anchorMin = new Vector2(0.5f, 0.5f);
            cancelRect.anchorMax = new Vector2(0.5f, 0.5f);
            cancelRect.pivot = new Vector2(0.5f, 0.5f);
            cancelRect.anchoredPosition = new Vector2(110.0f, -70.0f);

            this.clearConfirmPanel.SetActive(false);
        }

        private void ShowClearConfirmPanel(int archiveIndex)
        {
            if (!ArchiveManager.Instance.HasArchive(archiveIndex))
            {
                GlobalInit.Instance.ShowTip("空存档槽不能清除");
                this.RefreshArchiveSlotButtons();
                return;
            }

            this.pendingClearArchiveIndex = archiveIndex;
            string displayName = ArchiveManager.Instance.GetArchiveDisplayName(archiveIndex);
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

            if (ArchiveManager.Instance.DeleteArchive(this.pendingClearArchiveIndex))
            {
                GlobalInit.Instance.ShowTip("存档已清除");
            }
            else
            {
                GlobalInit.Instance.ShowTip("清除存档失败");
            }

            this.HideClearConfirmPanel();
            this.RefreshArchiveSlotButtons();
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
