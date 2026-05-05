namespace LAB2D
{
    using System.Collections.Generic;
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UI;

    /// <summary>
    /// 游戏前景面板
    /// </summary>
    public class ForegroundPanel : ABasePanel<ForegroundPanel>
    {
        private const float SaveSlotSpacing = 12.0f;
        private static readonly Vector2 SaveSlotSize = new (560.0f, 72.0f);
        private static readonly Vector2 SaveSlotViewportSize = new (640.0f, 360.0f);
        private readonly List<Button> saveSlotButtons = new ();
        private GameObject saveSlotPanel;
        private GameObject overwriteConfirmPanel;
        private Text overwriteConfirmText;
        private RectTransform saveSlotContent;
        private Font uiFont;
        private int pendingOverwriteArchiveIndex = -1;
        private GameObject clearConfirmPanel;
        private Text clearConfirmText;
        private int pendingClearArchiveIndex = -1;

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

            this.uiFont = this.GetUIFont();
            this.CreateSaveSlotPanel();
            this.CreateClearConfirmPanel();

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
            this.ShowSaveSlotPanel();
        }

        private void CreateSaveSlotPanel()
        {
            if (this.BindSceneSaveSlotPanel())
            {
                return;
            }

            this.CreateRuntimeSaveSlotPanel();
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

            this.ConfigureSceneSaveSlotViewport(viewportTransform);

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

        private void ConfigureSceneSaveSlotViewport(Transform viewportTransform)
        {
            RectTransform viewportRect = viewportTransform.GetComponent<RectTransform>();
            ScrollRect scrollRect = viewportTransform.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 50.0f;
            scrollRect.viewport = viewportRect;

            viewportTransform.GetComponent<Image>().raycastTarget = true;

            if (viewportTransform.GetComponent<RectMask2D>() == null)
            {
                viewportTransform.gameObject.AddComponent<RectMask2D>();
            }
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
            for (int i = 0; i < ArchiveManager.Instance.ArchiveCount; i++)
            {
                int archiveIndex = i;
                Button button = this.CreateSaveSlotButton(archiveIndex);
                this.saveSlotButtons.Add(button);
            }
        }

        private Button CreateSaveSlotButton(int archiveIndex)
        {
            GameObject gameObject = ResourceManager.Instance.Instantiate(PrefabConstant.ARCHIVE_ITEM, this.saveSlotContent, false);
            gameObject.name = $"SaveSlot_{archiveIndex + 1}";
            gameObject.layer = this.saveSlotContent.gameObject.layer;
            Button button = this.FindChildComponent<Button>(gameObject.transform, "Save");
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => this.OnClick_SaveSlot(archiveIndex));

            Button renameButton = this.FindChildComponent<Button>(gameObject.transform, "Rename");

            Button clearButton = this.FindChildComponent<Button>(gameObject.transform, "Clear");
            clearButton.onClick.RemoveAllListeners();
            clearButton.onClick.AddListener(() => this.ShowClearConfirmPanel(archiveIndex));

            return button;
        }

        private void BindSceneOverwriteConfirmPanel(Transform panelTransform)
        {
            Transform confirmPanelTransform = this.FindChildTransform(panelTransform, "OverwriteConfirmPanel");
            if (confirmPanelTransform == null)
            {
                this.CreateRuntimeOverwriteConfirmPanel();
                return;
            }

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

        private void CreateRuntimeSaveSlotPanel()
        {
            this.saveSlotPanel = this.CreateUIObject("SaveSlotPanel", this.Panel.transform);
            RectTransform panelRect = this.saveSlotPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image background = this.saveSlotPanel.AddComponent<Image>();
            background.color = PixelUITheme.ModalShade;
            background.raycastTarget = true;

            GameObject title = this.CreateText("Title", this.saveSlotPanel.transform, "选择存档槽", 40, TextAnchor.MiddleCenter);
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0.0f, 250.0f);
            titleRect.sizeDelta = new Vector2(600.0f, 70.0f);

            this.saveSlotContent = this.CreateRuntimeSaveSlotScrollView();
            this.CreateSaveSlotButtons();

            Button closeButton = this.CreateButton(
                "Close",
                this.saveSlotPanel.transform,
                "关闭",
                new Vector2(180.0f, 58.0f),
                this.HideSaveSlotPanel);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.5f, 0.5f);
            closeRect.anchorMax = new Vector2(0.5f, 0.5f);
            closeRect.pivot = new Vector2(0.5f, 0.5f);
            closeRect.anchoredPosition = new Vector2(0.0f, -270.0f);

            this.CreateRuntimeOverwriteConfirmPanel();
            this.saveSlotPanel.SetActive(false);
        }

        private RectTransform CreateRuntimeSaveSlotScrollView()
        {
            GameObject viewport = this.CreateUIObject("SaveSlotViewport", this.saveSlotPanel.transform);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0.5f, 0.5f);
            viewportRect.anchorMax = new Vector2(0.5f, 0.5f);
            viewportRect.pivot = new Vector2(0.5f, 0.5f);
            viewportRect.anchoredPosition = new Vector2(0.0f, 20.0f);
            viewportRect.sizeDelta = SaveSlotViewportSize;

            Image image = viewport.AddComponent<Image>();
            image.color = PixelUITheme.ViewportBg;
            image.raycastTarget = true;

            viewport.AddComponent<RectMask2D>();
            ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 50.0f;
            scrollRect.viewport = viewportRect;

            GameObject content = this.CreateUIObject("SaveSlotContent", viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 1.0f);
            contentRect.anchorMax = new Vector2(0.5f, 1.0f);
            contentRect.pivot = new Vector2(0.5f, 1.0f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = this.GetSaveSlotContentSize();
            scrollRect.content = contentRect;
            return contentRect;
        }

        private Vector2 GetSaveSlotContentSize()
        {
            int count = ArchiveManager.Instance.ArchiveCount;
            float height = (count * SaveSlotSize.y) + (Mathf.Max(0, count - 1) * SaveSlotSpacing);
            return new Vector2(SaveSlotSize.x, height);
        }

        private void CreateRuntimeOverwriteConfirmPanel()
        {
            this.overwriteConfirmPanel = this.CreateUIObject("OverwriteConfirmPanel", this.saveSlotPanel.transform);
            RectTransform panelRect = this.overwriteConfirmPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image shade = this.overwriteConfirmPanel.AddComponent<Image>();
            shade.color = PixelUITheme.DialogShadeDark;
            shade.raycastTarget = true;

            GameObject box = this.CreateUIObject("Box", this.overwriteConfirmPanel.transform);
            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.anchoredPosition = Vector2.zero;
            boxRect.sizeDelta = new Vector2(560.0f, 260.0f);

            Image boxImage = box.AddComponent<Image>();
            boxImage.color = PixelUITheme.DialogBoxBg;
            boxImage.raycastTarget = true;

            GameObject tip = this.CreateText("Tip", box.transform, string.Empty, 32, TextAnchor.MiddleCenter);
            tip.GetComponent<Text>().color = PixelUITheme.TextPrimary;
            RectTransform tipRect = tip.GetComponent<RectTransform>();
            tipRect.anchorMin = new Vector2(0.5f, 0.5f);
            tipRect.anchorMax = new Vector2(0.5f, 0.5f);
            tipRect.pivot = new Vector2(0.5f, 0.5f);
            tipRect.anchoredPosition = new Vector2(0.0f, 55.0f);
            tipRect.sizeDelta = new Vector2(500.0f, 110.0f);
            this.overwriteConfirmText = tip.GetComponent<Text>();

            Button confirmButton = this.CreateButton(
                "Confirm",
                box.transform,
                "确认覆盖",
                new Vector2(190.0f, 58.0f),
                this.OnClick_ConfirmOverwrite);
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
                this.HideOverwriteConfirmPanel);
            RectTransform cancelRect = cancelButton.GetComponent<RectTransform>();
            cancelRect.anchorMin = new Vector2(0.5f, 0.5f);
            cancelRect.anchorMax = new Vector2(0.5f, 0.5f);
            cancelRect.pivot = new Vector2(0.5f, 0.5f);
            cancelRect.anchoredPosition = new Vector2(110.0f, -70.0f);

            this.overwriteConfirmPanel.SetActive(false);
        }

        private void ShowSaveSlotPanel()
        {
            this.RefreshSaveSlotButtons();
            this.saveSlotPanel.SetActive(true);
            this.saveSlotPanel.transform.SetAsLastSibling();
        }

        private void HideSaveSlotPanel()
        {
            this.pendingOverwriteArchiveIndex = -1;
            this.HideOverwriteConfirmPanel();
            this.HideClearConfirmPanel();
            this.saveSlotPanel.SetActive(false);
        }

        private void RefreshSaveSlotButtons()
        {
            for (int i = 0; i < this.saveSlotButtons.Count; i++)
            {
                bool hasArchive = ArchiveManager.Instance.HasArchive(i);
                Text text = this.FindChildComponent<Text>(this.saveSlotButtons[i].transform, "Text");
                string displayName = ArchiveManager.Instance.GetArchiveDisplayName(i);
                string status = hasArchive ? "已有存档" : "空槽";
                text.text = $"{displayName}\n{status}";
            }
        }

        private void OnClick_SaveSlot(int archiveIndex)
        {
            if (ArchiveManager.Instance.HasArchive(archiveIndex))
            {
                this.ShowOverwriteConfirmPanel(archiveIndex);
                return;
            }

            this.SaveToArchive(archiveIndex);
        }

        private void ShowOverwriteConfirmPanel(int archiveIndex)
        {
            this.pendingOverwriteArchiveIndex = archiveIndex;
            string displayName = ArchiveManager.Instance.GetArchiveDisplayName(archiveIndex);
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
            Transform panelTransform = this.FindChildTransform(this.saveSlotPanel.transform, "ClearConfirmPanel");
            if (panelTransform != null)
            {
                this.clearConfirmPanel = panelTransform.gameObject;
                this.clearConfirmText = this.FindChildComponent<Text>(panelTransform, "Tip");

                Button confirmButton = this.FindChildComponent<Button>(panelTransform, "Confirm");
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(this.OnClick_ConfirmClear);

                Button cancelButton = this.FindChildComponent<Button>(panelTransform, "Cancel");
                cancelButton.onClick.RemoveAllListeners();
                cancelButton.onClick.AddListener(this.HideClearConfirmPanel);

                this.clearConfirmPanel.SetActive(false);
                return;
            }

            this.CreateRuntimeClearConfirmPanel();
        }

        private void CreateRuntimeClearConfirmPanel()
        {
            this.clearConfirmPanel = this.CreateUIObject("ClearConfirmPanel", this.saveSlotPanel.transform);
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

            GameObject tip = this.CreateText("Tip", box.transform, string.Empty, 32, TextAnchor.MiddleCenter);
            tip.GetComponent<Text>().color = PixelUITheme.TextPrimary;
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
                this.RefreshSaveSlotButtons();
                return;
            }

            this.pendingClearArchiveIndex = archiveIndex;
            string displayName = ArchiveManager.Instance.GetArchiveDisplayName(archiveIndex);
            this.clearConfirmText.text = $"确认清除存档\n{displayName}?";
            this.clearConfirmPanel.SetActive(true);
            this.clearConfirmPanel.transform.SetAsLastSibling();
        }

        private void HideClearConfirmPanel()
        {
            this.pendingClearArchiveIndex = -1;
            this.clearConfirmPanel.SetActive(false);
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
            this.RefreshSaveSlotButtons();
        }

        private void SaveToArchive(int archiveIndex)
        {
            ArchiveManager.Instance.SetCurrentArchive(archiveIndex);
            GlobalInit.Instance.ShowTip($"保存数据: {ArchiveManager.Instance.CurrentArchiveDisplayName}");
            ArchiveManager.Instance.SaveCurrentArchive();
            this.HideSaveSlotPanel();
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
            image.color = PixelUITheme.ButtonNormal;

            Button button = gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = PixelUITheme.ButtonNormal;
            colors.highlightedColor = PixelUITheme.ButtonHighlighted;
            colors.pressedColor = PixelUITheme.ButtonPressed;
            colors.selectedColor = PixelUITheme.ButtonSelected;
            colors.disabledColor = PixelUITheme.ButtonDisabled;
            button.colors = colors;
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            GameObject textObject = this.CreateText("Text", gameObject.transform, text, 30, TextAnchor.MiddleCenter);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            return button;
        }

        private GameObject CreateText(string name, Transform parent, string text, int fontSize, TextAnchor alignment)
        {
            GameObject gameObject = this.CreateUIObject(name, parent);
            Text textComponent = gameObject.AddComponent<Text>();
            textComponent.font = this.uiFont;
            textComponent.fontSize = fontSize;
            textComponent.resizeTextForBestFit = true;
            textComponent.resizeTextMinSize = 14;
            textComponent.resizeTextMaxSize = fontSize;
            textComponent.alignment = alignment;
            textComponent.color = Color.white;
            textComponent.text = text;
            return gameObject;
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

        private void Onclick_GeneratorItem()
        {
            if (EnemyManager.Instance.Characters.Count > 0)
            {
                new EnemyDropManager().DropItem(PlayerManager.Instance.Mine.transform.position);
            }
        }
    }
}
