namespace LAB2D.UI.Panel
{
    using LAB2D;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 设置菜单面板
    /// 包含游戏速度、视角切换、边缘移动和按键操作说明。
    /// </summary>
    public class SettingMenuPanel : ABasePanel<SettingMenuPanel>
    {
        /// <summary>
        /// 按键说明文本组件（运行时创建）。
        /// </summary>
        private Text keyBindingText;

        public SettingMenuPanel()
        {
            this.Name = "SettingMenu";
            this.Init();

            // cameras = Object.FindObjectsOfType(typeof(Camera), true);
            LAB2D.Tool.Tool.GetComponentInChildren<Transform>(this.Panel, "Angle").Find("Toggle")
                .GetComponent<Toggle>().onValueChanged.AddListener(this.OnClick_TogglePerspective);
            LAB2D.Tool.Tool.GetComponentInChildren<Transform>(this.Panel, "Edge").Find("Toggle")
               .GetComponent<Toggle>().onValueChanged.AddListener(this.OnClick_ToggleEdgeMove);
            LAB2D.Tool.Tool.GetComponentInChildren<Transform>(this.Panel, "Speed").Find("Slider")
               .GetComponent<Slider>().onValueChanged.AddListener(this.OnClick_GameSpeed);
            LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.Panel, "BackGame").onClick.AddListener(this.OnClick_Back);

            this.CreateKeyBindingUI();
        }

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

        /// <summary>
        /// 返回游戏
        /// </summary>
        public override void OnClick_Back()
        {
            this.Controller.Close();
        }

        /// <summary>
        /// 创建按键说明 UI 文本。
        /// 在设置面板底部显示所有按键操作说明，方便玩家查阅。
        /// </summary>
        private void CreateKeyBindingUI()
        {
            if (this.Panel == null)
            {
                return;
            }

            GameObject keyGo = new GameObject("KeyBindingInfo", typeof(RectTransform));
            keyGo.transform.SetParent(this.Panel.transform, false);
            this.keyBindingText = keyGo.AddComponent<Text>();
            this.keyBindingText.text = InputKeyConstant.GetKeyBindingSummary();
            this.keyBindingText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            this.keyBindingText.fontSize = 14;
            this.keyBindingText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            this.keyBindingText.alignment = TextAnchor.UpperLeft;
            this.keyBindingText.raycastTarget = false;

            RectTransform rt = this.keyBindingText.rectTransform;
            rt.anchorMin = new Vector2(0.2f, 0.02f);
            rt.anchorMax = new Vector2(0.8f, 0.7f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 切换视角(2.5D)
        /// </summary>
        private void OnClick_TogglePerspective(bool is2D)
        {
            PlayerManager.Instance.Mine.TogglePerspective(is2D);
        }

        /// <summary>
        /// 切换边缘移动
        /// </summary>
        private void OnClick_ToggleEdgeMove(bool isEdge)
        {
            CameraMove.IsEdgeMode = isEdge;
        }

        private void OnClick_GameSpeed(float speed)
        {
            ForegroundPanel.Instance.TimeScale = speed;
        }
    }
}