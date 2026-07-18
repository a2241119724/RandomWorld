namespace LAB2D.Tool
{
    using System.Reflection;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// HUD 公共工厂方法 — 消除各 HUD EnsureRuntimePanel 中的重复代码。
    /// </summary>
    public static class HudFactory
    {
        /// <summary>
        /// 查找 HUD 应挂载的父节点。
        /// 优先返回 UIRoot/Foreground，其次 UIRoot，都不存在时创建独立 Canvas 兜底。
        /// </summary>
        /// <returns>父节点 Transform。</returns>
        public static Transform FindHudParent()
        {
            GameObject uiRoot = GameObject.FindGameObjectWithTag(TagConstant.UI_TAG);
            if (uiRoot != null)
            {
                Transform foreground = uiRoot.transform.Find("Foreground");
                return foreground != null ? foreground : uiRoot.transform;
            }

            return new GameObject(
                "HUD_Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)).transform;
        }

        /// <summary>
        /// 修复场景中已有的 HUD 实例状态。
        /// 场景序列化可能残留 toggleKey=None、enabled=false、GameObject inactive 等错误状态。
        /// </summary>
        /// <param name="hud">HUD MonoBehaviour 实例。</param>
        /// <param name="toggleKey">正确的热键值。</param>
        /// <param name="defaultVisible">修复后默认可见状态。</param>
        public static void RepairExisting(MonoBehaviour hud, KeyCode toggleKey, bool defaultVisible = true)
        {
            // 通过反射设置 toggleKey 字段（所有 HUD 均有此 public 字段）
            FieldInfo field = hud.GetType().GetField("toggleKey");
            if (field != null)
            {
                field.SetValue(hud, toggleKey);
            }

            hud.enabled = true;
            if (!hud.gameObject.activeSelf)
            {
                hud.gameObject.SetActive(true);
            }

            // 通过反射调用 SetVisible()
            System.Reflection.MethodInfo method = hud.GetType().GetMethod("SetVisible");
            if (method != null)
            {
                method.Invoke(hud, new object[] { defaultVisible });
            }
        }
    }
}
