namespace LAB2D.UI
{
    using LAB2D;
    using LAB2D.Enum;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 浮动文字表现组件
    /// 挂载在每个浮动文字 GameObject 上，驱动上浮、弹出缩放、淡出动画，并在动画结束后自动回收到对象池。
    ///
    /// 生命周期：EnsureReady（懒初始化）→ Spawn（设置内容）→ 弹出动画（可选）→ 上浮 → 淡出 → 回收
    /// 由 FloatingTextManager 统一管理分配和回收。
    ///
    /// 注意：对象创建时父节点 poolContainer 为 inactive，因此 Awake 可能被延迟执行。
    /// 所有组件访问前必须通过 EnsureReady() 保证懒初始化已完成。
    /// </summary>
    public class FloatingTextUI : MonoBehaviour
    {
        private Text textComponent;
        private Outline outline;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private bool componentsReady;

        private FloatingTextType currentType;
        private float elapsedTime;
        private float lifetime;
        private float floatSpeed;
        private float popScale;
        private bool isActive;

        // 弹出动画状态
        private bool popAnimating;
        private float popElapsed;

        // 屏幕空间起始位置（用于上浮计算）
        private Vector2 screenStartPos;

        /// <summary>
        /// 确保所有 UI 组件已初始化（懒初始化，幂等）
        /// 无论 Awake 是否已执行，调用后所有组件引用均有效。
        /// </summary>
        private void EnsureReady()
        {
            if (this.componentsReady)
            {
                return;
            }

            this.textComponent = this.GetComponent<Text>();
            if (this.textComponent == null)
            {
                this.textComponent = this.gameObject.AddComponent<Text>();
            }

            this.textComponent.alignment = TextAnchor.MiddleCenter;
            this.textComponent.raycastTarget = false;

            this.outline = this.GetComponent<Outline>();
            if (this.outline == null)
            {
                this.outline = this.gameObject.AddComponent<Outline>();
                this.outline.effectColor = new Color(0, 0, 0, 0.5f);
                this.outline.effectDistance = new Vector2(1, -1);
            }

            this.rectTransform = this.GetComponent<RectTransform>();
            if (this.rectTransform == null)
            {
                this.rectTransform = this.gameObject.AddComponent<RectTransform>();
            }

            this.canvasGroup = this.GetComponent<CanvasGroup>();
            if (this.canvasGroup == null)
            {
                this.canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
            }

            this.componentsReady = true;
        }

        private void Awake()
        {
            // Awake 作为兜底：若父节点在创建时已激活，此处会正常初始化
            this.EnsureReady();
            this.gameObject.SetActive(false);
        }

        /// <summary>
        /// 初始化浮动文字并开始动画
        /// </summary>
        /// <param name="type">文字类型</param>
        /// <param name="text">显示文本</param>
        /// <param name="screenPos">屏幕坐标位置</param>
        public void Spawn(FloatingTextType type, string text, Vector2 screenPos)
        {
            // 懒初始化：无论 Awake 是否已执行，确保组件可用
            this.EnsureReady();

            this.currentType = type;
            this.elapsedTime = 0f;
            this.popElapsed = 0f;
            this.isActive = true;

            // 设置文字内容和样式
            this.textComponent.text = text;
            this.textComponent.color = FloatingTextTool.GetColor(type);
            this.textComponent.fontSize = FloatingTextTool.GetFontSize(type);

            // 获取动画参数
            this.lifetime = FloatingTextTool.GetLifetime(type);
            this.floatSpeed = FloatingTextTool.GetFloatSpeed(type);
            this.popScale = FloatingTextTool.GetPopScale(type);

            // 设置屏幕位置
            this.screenStartPos = screenPos;
            this.rectTransform.anchoredPosition = screenPos;

            // 重置透明度和缩放
            this.canvasGroup.alpha = 1f;
            this.transform.localScale = Vector3.one;

            // 暴击和连击类型有弹出动画
            this.popAnimating = this.popScale > 1.0f;

            this.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!this.isActive)
            {
                return;
            }

            this.elapsedTime += Time.deltaTime;

            // 弹出动画阶段
            if (this.popAnimating)
            {
                this.popElapsed += Time.deltaTime;
                float popProgress = Mathf.Clamp01(this.popElapsed / FloatingTextConstant.PopAnimDuration);

                if (popProgress >= 1f)
                {
                    // 弹出结束，回到正常大小
                    this.transform.localScale = Vector3.one;
                    this.popAnimating = false;
                }
                else
                {
                    // 从 1 到 popScale 再回到 1（使用 sin 曲线模拟弹跳）
                    float scale = 1f + ((this.popScale - 1f) * Mathf.Sin(popProgress * Mathf.PI));
                    this.transform.localScale = new Vector3(scale, scale, 1f);
                }
            }

            // 上浮动画
            float floatOffset = this.floatSpeed * this.elapsedTime * 60f; // 转换为屏幕像素偏移
            this.rectTransform.anchoredPosition = new Vector2(
                this.screenStartPos.x,
                this.screenStartPos.y + floatOffset);

            // 淡出阶段
            float fadeStartTime = this.lifetime - FloatingTextConstant.FadeOutDuration;
            if (this.elapsedTime >= fadeStartTime && this.elapsedTime < this.lifetime + FloatingTextConstant.FadeOutDuration)
            {
                float fadeProgress = (this.elapsedTime - fadeStartTime) / FloatingTextConstant.FadeOutDuration;
                this.canvasGroup.alpha = 1f - Mathf.Clamp01(fadeProgress);
            }

            // 生命周期结束，回收
            if (this.elapsedTime >= this.lifetime + FloatingTextConstant.FadeOutDuration)
            {
                this.Recycle();
            }
        }

        /// <summary>
        /// 回收此浮动文字到对象池
        /// </summary>
        public void Recycle()
        {
            this.isActive = false;
            this.popAnimating = false;
            this.gameObject.SetActive(false);
            FloatingTextManager.Instance.ReturnToPool(this);
        }

        /// <summary>
        /// 立即停止并回收（用于清场）
        /// </summary>
        public void ForceRecycle()
        {
            this.EnsureReady();
            this.isActive = false;
            this.popAnimating = false;
            this.canvasGroup.alpha = 0f;
            this.gameObject.SetActive(false);
        }
    }
}
