namespace LAB2D.UI
{
    using LAB2D;
    using LAB2D.Constant;
    using LAB2D.Domain.Common;
    using LAB2D.Gameplay;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 成就解锁弹窗
    /// 用途：在成就解锁时显示浮动通知弹窗，展示成就名称、描述和点数奖励，一段时间后自动隐藏。
    /// 由 AchievementManager.OnAchievementUnlocked 事件驱动。
    ///
    /// 接入方式：
    ///   AchievementPopup 挂载到 UI/Foreground 下，复用 UI 的 Canvas。
    ///   AchievementManager 解锁成就时会自动触发弹窗显示。
    ///   提供静态方法 EnsureRuntimePopup() 安全创建运行时弹窗实例。
    /// </summary>
    public class AchievementPopup : MonoBehaviour
    {
        /// <summary>运行时弹窗单例引用</summary>
        private static AchievementPopup runtimeInstance;

        private CanvasGroup canvasGroup;
        private Text titleText;
        private Text nameText;
        private Text pointsText;
        private Image backgroundImage;
        private RectTransform rootRect;
        private Coroutine autoHideCoroutine;

        /// <summary>
        /// 确保运行时弹窗存在。若不存在则动态创建。
        /// 由 GlobalInit.Start 或 游戏启动时调用。
        /// </summary>
        public static void EnsureRuntimePopup()
        {
            if (runtimeInstance != null)
            {
                return;
            }

            Transform foreground = AchievementTool.FindForeground();
            Transform popupTransform = foreground?.Find(AchievementConstant.PopupRootName);
            if (popupTransform != null)
            {
                runtimeInstance = popupTransform.GetComponent<AchievementPopup>();
                if (runtimeInstance != null)
                {
                    runtimeInstance.EnsureReferences();
                }
                else
                {
                    Debug.LogWarning($"[AchievementPopup] 场景中存在 {AchievementConstant.PopupRootName} 但未挂载 AchievementPopup 组件。");
                }

                return;
            }

            Debug.LogWarning($"[AchievementPopup] 在 Foreground 下未找到 {AchievementConstant.PopupRootName}，请手动创建。");
        }

        /// <summary>
        /// 获取运行时弹窗实例
        /// </summary>
        public static AchievementPopup RuntimeInstance
        {
            get { return runtimeInstance; }
        }

        private void Awake()
        {
            this.EnsureReferences();
        }

        public void EnsureReferences()
        {
            if (this.nameText != null) return;

            this.canvasGroup = this.GetComponent<CanvasGroup>();
            this.rootRect = this.GetComponent<RectTransform>();
            this.backgroundImage = this.GetComponent<Image>();
            this.titleText = this.transform.Find("Title")?.GetComponent<Text>();
            this.nameText = this.transform.Find("Name")?.GetComponent<Text>();
            this.pointsText = this.transform.Find("Points")?.GetComponent<Text>();
        }

        private void Initialize()
        {
            this.canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
            this.rootRect = this.gameObject.AddComponent<RectTransform>();

            // 弹窗布局：右上角，适中大小
            this.rootRect.anchorMin = new Vector2(1, 1);
            this.rootRect.anchorMax = new Vector2(1, 1);
            this.rootRect.pivot = new Vector2(1, 1);
            this.rootRect.anchoredPosition = new Vector2(-20, -20);
            this.rootRect.sizeDelta = new Vector2(280, 120);

            // 背景
            this.backgroundImage = this.gameObject.AddComponent<Image>();
            this.backgroundImage.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

            // 标题 "成就解锁！"
            this.titleText = AchievementTool.CreateText(
                this.transform, "Title", AchievementConstant.DefaultUnlockTitle, 20);
            RectTransform titleRt = this.titleText.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0.5f, 1);
            titleRt.anchoredPosition = new Vector2(0, -10);
            titleRt.sizeDelta = new Vector2(0, 30);
            this.titleText.alignment = TextAnchor.MiddleCenter;
            this.titleText.color = new Color(1f, 0.85f, 0.3f); // 金色

            // 成就名称
            this.nameText = AchievementTool.CreateText(
                this.transform, "Name", "", 18);
            RectTransform nameRt = this.nameText.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 1);
            nameRt.anchorMax = new Vector2(1, 1);
            nameRt.pivot = new Vector2(0.5f, 1);
            nameRt.anchoredPosition = new Vector2(0, -40);
            nameRt.sizeDelta = new Vector2(0, 25);
            this.nameText.alignment = TextAnchor.MiddleCenter;
            this.nameText.color = Color.white;

            // 点数
            this.pointsText = AchievementTool.CreateText(
                this.transform, "Points", "", 16);
            RectTransform pointsRt = this.pointsText.GetComponent<RectTransform>();
            pointsRt.anchorMin = new Vector2(0, 0);
            pointsRt.anchorMax = new Vector2(1, 0);
            pointsRt.pivot = new Vector2(0.5f, 0);
            pointsRt.anchoredPosition = new Vector2(0, 15);
            pointsRt.sizeDelta = new Vector2(0, 25);
            this.pointsText.alignment = TextAnchor.MiddleCenter;
            this.pointsText.color = new Color(0.3f, 1f, 0.5f); // 绿色

            // 初始透明
            this.canvasGroup.alpha = 0f;
        }

        /// <summary>
        /// 显示解锁弹窗
        /// </summary>
        /// <param name="data">已解锁的成就数据</param>
        public void Show(AchievementData data)
        {
            if (data == null)
            {
                return;
            }

            this.nameText.text = data.Name;
            this.pointsText.text = data.PointsText;

            // 确保 GameObject 及其所有父级在层级中激活，否则 StartCoroutine 会报错
            // 仅 SetActive(true) 自身不够 —— 若父级 inactive，activeInHierarchy 仍为 false
            if (!this.gameObject.activeInHierarchy)
            {
                // 收集自身及所有未激活的祖先（从自身往上），再从顶层往下逐级激活
                var inactiveChain = new System.Collections.Generic.List<GameObject>();
                Transform t = this.transform;
                while (t != null)
                {
                    if (!t.gameObject.activeSelf)
                        inactiveChain.Add(t.gameObject);
                    t = t.parent;
                }

                for (int i = inactiveChain.Count - 1; i >= 0; i--)
                {
                    inactiveChain[i].SetActive(true);
                }
            }

            // 重置为完全透明并恢复交互，协程从淡入开始
            if (this.canvasGroup != null)
            {
                this.canvasGroup.alpha = 0f;
                this.canvasGroup.blocksRaycasts = true;
                this.canvasGroup.interactable = true;
            }

            // 停掉之前的自动隐藏协程
            if (this.autoHideCoroutine != null)
            {
                this.StopCoroutine(this.autoHideCoroutine);
            }

            this.autoHideCoroutine = this.StartCoroutine(this.AutoHideRoutine());
        }

        /// <summary>
        /// 淡入 → 保持 → 淡出 自动流程
        /// </summary>
        private IEnumerator AutoHideRoutine()
        {
            // 淡入
            float fadeIn = AchievementConstant.PopupFadeInDuration;
            float elapsed = 0f;
            while (elapsed < fadeIn)
            {
                elapsed += Time.deltaTime;
                this.canvasGroup.alpha = MathHelper.Clamp01(elapsed / fadeIn);
                yield return null;
            }

            this.canvasGroup.alpha = 1f;

            // 保持
            yield return new WaitForSeconds(AchievementConstant.PopupAutoHideDelay);

            // 淡出
            float fadeOut = AchievementConstant.PopupFadeOutDuration;
            elapsed = 0f;
            while (elapsed < fadeOut)
            {
                elapsed += Time.deltaTime;
                this.canvasGroup.alpha = MathHelper.Clamp01(1f - (elapsed / fadeOut));
                yield return null;
            }

            this.canvasGroup.alpha = 0f;

            // 从队列移除已展示成就
            if (Core.ServiceLocator.TryGet(out AchievementManager am)) { am.DequeuePendingUnlock(); }

            // 弹窗播放完毕，禁用交互及射线阻挡，避免干扰底层 UI
            // 不再使用 SetActive(false)，防止下次 Show 时因父级 inactive 导致 StartCoroutine 失败
            if (this.canvasGroup != null)
            {
                this.canvasGroup.blocksRaycasts = false;
                this.canvasGroup.interactable = false;
            }
        }
    }
}
