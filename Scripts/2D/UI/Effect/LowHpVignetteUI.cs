namespace LAB2D.UI.Effect
{
    using LAB2D.Character.Worker.Task;
    using LAB2D.Manager;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 低血量红晕（玩家专用）— 全屏 vignette overlay，代码创建零 prefab。
    /// 血量低于阈值时屏幕四边泛红并呼吸脉冲（Custom/LowHpVignette shader），
    /// 强度随血量趋零增强；高于阈值时隐藏（Canvas SetActive false，零 overdraw）。
    /// 由 PlayerViewAdapter.Tick 每帧喂血量百分比（读一个 float，成本可忽略）。
    /// </summary>
    public static class LowHpVignetteUI
    {
        /// <summary>血量百分比阈值（低于开始泛红）</summary>
        private const float Threshold = 0.30f;

        private const string ShaderName = "Custom/LowHpVignette";
        private const string CanvasName = "LowHpVignetteCanvas";

        private static Material vignetteMaterial;
        private static GameObject overlayRoot;

        /// <summary>
        /// 喂入玩家当前血量百分比（Hp/MaxHp），驱动红晕强度。
        /// 首次低于阈值时懒创建 overlay（shader 缺失则建占位空对象防重复查找，仅记一次 Warning）。
        /// </summary>
        public static void SetHealthPercent(float hpPercent)
        {
            float t = Mathf.Clamp01((Threshold - hpPercent) / Threshold);

            if (t <= 0f)
            {
                if (overlayRoot != null && overlayRoot.activeSelf)
                {
                    overlayRoot.SetActive(false); // 零 overdraw
                }

                return;
            }

            EnsureCreated();
            if (vignetteMaterial == null)
            {
                return;
            }

            if (!overlayRoot.activeSelf)
            {
                overlayRoot.SetActive(true);
            }

            // 刚低于阈值温和、濒死强烈的增强曲线
            vignetteMaterial.SetFloat("_Intensity", Mathf.Pow(t, 1.5f));
        }

        private static void EnsureCreated()
        {
            if (overlayRoot != null)
            {
                return;
            }

            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                AWorkerTask.LogProvider($"未找到 {ShaderName}，低血量红晕不可用", LogManager.LogLevelEnum.Warning);
                overlayRoot = new GameObject(CanvasName); // 占位防每帧重复 Shader.Find
                Object.DontDestroyOnLoad(overlayRoot);
                return;
            }

            vignetteMaterial = new Material(shader);

            // 独立顶层 Screen Space Canvas，不依赖场景 UI 结构；
            // Image 无 sprite 时 Graphic.mainTexture = s_WhiteTexture，CanvasRenderer 自动绑定 _MainTex
            GameObject root = new GameObject(CanvasName);
            Object.DontDestroyOnLoad(root);
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767;

            GameObject imageGo = new GameObject("Vignette");
            imageGo.transform.SetParent(root.transform, false);
            Image image = imageGo.AddComponent<Image>();
            image.material = vignetteMaterial;
            image.raycastTarget = false; // 关键：不挡任何输入

            RectTransform rt = imageGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            overlayRoot = root;
            overlayRoot.SetActive(false); // 初建隐藏，等血量达标再显示
        }
    }
}
