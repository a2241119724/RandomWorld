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
        private const int SaveSlotColumnCount = 2;
        private static readonly Vector2 SaveSlotSize = new (280.0f, 72.0f);
        private static readonly Vector2 SaveSlotSpacing = new (18.0f, 14.0f);
        private readonly List<Button> saveSlotButtons = new ();
        private GameObject saveSlotPanel;
        private GameObject overwriteConfirmPanel;
        private Text overwriteConfirmText;
        private Font uiFont;
        private int pendingOverwriteArchiveIndex = -1;

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
            this.saveSlotPanel = this.CreateUIObject("SaveSlotPanel", this.Panel.transform);
            RectTransform panelRect = this.saveSlotPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image background = this.saveSlotPanel.AddComponent<Image>();
            background.color = new Color(0.0f, 0.0f, 0.0f, 0.55f);
            background.raycastTarget = true;

            GameObject title = this.CreateText("Title", this.saveSlotPanel.transform, "选择存档槽", 40, TextAnchor.MiddleCenter);
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0.0f, 250.0f);
            titleRect.sizeDelta = new Vector2(600.0f, 70.0f);

            for (int i = 0; i < ArchiveManager.Instance.ArchiveCount; i++)
            {
                int archiveIndex = i;
                Button button = this.CreateButton(
                    $"SaveSlot_{archiveIndex + 1}",
                    this.saveSlotPanel.transform,
                    string.Empty,
                    SaveSlotSize,
                    () => this.OnClick_SaveSlot(archiveIndex));
                this.SetSaveSlotButtonPosition(button, archiveIndex);
                this.saveSlotButtons.Add(button);
            }

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

            this.CreateOverwriteConfirmPanel();
            this.saveSlotPanel.SetActive(false);
        }

        private void CreateOverwriteConfirmPanel()
        {
            this.overwriteConfirmPanel = this.CreateUIObject("OverwriteConfirmPanel", this.saveSlotPanel.transform);
            RectTransform panelRect = this.overwriteConfirmPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image shade = this.overwriteConfirmPanel.AddComponent<Image>();
            shade.color = new Color(0.0f, 0.0f, 0.0f, 0.72f);
            shade.raycastTarget = true;

            GameObject box = this.CreateUIObject("Box", this.overwriteConfirmPanel.transform);
            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.anchoredPosition = Vector2.zero;
            boxRect.sizeDelta = new Vector2(560.0f, 260.0f);

            Image boxImage = box.AddComponent<Image>();
            boxImage.color = new Color(0.12f, 0.13f, 0.15f, 0.96f);
            boxImage.raycastTarget = true;

            GameObject tip = this.CreateText("Tip", box.transform, string.Empty, 32, TextAnchor.MiddleCenter);
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
            this.saveSlotPanel.SetActive(false);
        }

        private void RefreshSaveSlotButtons()
        {
            for (int i = 0; i < this.saveSlotButtons.Count; i++)
            {
                bool hasArchive = ArchiveManager.Instance.HasArchive(i);
                Text text = this.saveSlotButtons[i].GetComponentInChildren<Text>(true);
                if (text != null)
                {
                    string status = hasArchive ? "已有存档" : "空槽";
                    text.text = $"存档 {i + 1}\n{status}";
                }
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
            if (this.overwriteConfirmText != null)
            {
                this.overwriteConfirmText.text = $"存档 {archiveIndex + 1} 已存在\n是否确认覆盖?";
            }

            this.overwriteConfirmPanel.SetActive(true);
            this.overwriteConfirmPanel.transform.SetAsLastSibling();
        }

        private void HideOverwriteConfirmPanel()
        {
            if (this.overwriteConfirmPanel != null)
            {
                this.overwriteConfirmPanel.SetActive(false);
            }
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

        private void SaveToArchive(int archiveIndex)
        {
            ArchiveManager.Instance.SetCurrentArchive(archiveIndex);
            GlobalInit.Instance.ShowTip($"保存数据: 存档 {archiveIndex + 1}");
            ArchiveManager.Instance.SaveCurrentArchive();
            this.HideSaveSlotPanel();
        }

        private void SetSaveSlotButtonPosition(Button saveSlotButton, int archiveIndex)
        {
            RectTransform rectTransform = saveSlotButton.GetComponent<RectTransform>();
            int row = archiveIndex / SaveSlotColumnCount;
            int column = archiveIndex % SaveSlotColumnCount;
            float x = (column - ((SaveSlotColumnCount - 1) * 0.5f)) * (SaveSlotSize.x + SaveSlotSpacing.x);
            float y = 165.0f - (row * (SaveSlotSize.y + SaveSlotSpacing.y));
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(x, y);
            rectTransform.sizeDelta = SaveSlotSize;
            rectTransform.localScale = Vector3.one;
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
            image.color = new Color(0.20f, 0.60f, 0.86f, 1.0f);

            Button button = gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.20f, 0.60f, 0.86f, 1.0f);
            colors.highlightedColor = new Color(0.36f, 0.68f, 0.89f, 1.0f);
            colors.pressedColor = new Color(0.95f, 0.77f, 0.06f, 1.0f);
            colors.selectedColor = new Color(0.18f, 0.80f, 0.44f, 1.0f);
            colors.disabledColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
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
            Text text = Tool.GetComponentInChildren<Text>(this.Panel);
            if (text != null && text.font != null)
            {
                return text.font;
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
