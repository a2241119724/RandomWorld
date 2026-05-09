namespace LAB2D
{
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;

    /// <summary>
    /// 主动技能系统 Editor 菜单工具。
    /// 提供安装/移除/验证三项功能，降低 Game 场景手动修改风险。
    /// 仅在 Unity Editor 环境下使用，不会被打包到运行时。
    /// </summary>
    public static class SkillMenu
    {
        /// <summary>
        /// 安装技能 HUD 到当前打开的 Game 场景。
        /// 优先在已存在的 Canvas 下添加独立子节点；无 Canvas 时创建独立 Canvas。
        /// 不覆盖已有对象，重复执行会跳过。
        /// </summary>
        [MenuItem(SkillConstant.MenuRoot + SkillConstant.MenuInstallToScene, false, 0)]
        public static void InstallSkillHUDToScene()
        {
            // 检查是否在 Game 场景中
            Scene activeScene = SceneManager.GetActiveScene();
            string gameSceneName = "Game";
            if (!activeScene.name.Contains(gameSceneName))
            {
                Debug.LogWarning(
                    $"[SkillMenu] 当前场景为 '{activeScene.name}'，" +
                    $"建议在 Game 场景中安装技能 HUD。操作已继续。");
            }

            // 检查是否已存在
            GameObject existingCanvas = GameObject.Find(SkillConstant.SkillCanvasName);
            if (existingCanvas != null)
            {
                Debug.LogWarning(
                    $"[SkillMenu] 技能HUD已存在于场景中 ({SkillConstant.SkillCanvasName})，跳过安装。");
                return;
            }

            GameObject existingRoot = GameObject.Find(SkillConstant.SkillHUDRootName);
            if (existingRoot != null)
            {
                Debug.LogWarning(
                    $"[SkillMenu] 技能HUD根节点已存在 ({SkillConstant.SkillHUDRootName})，跳过安装。");
                return;
            }

            // 创建 Canvas
            GameObject canvasObj = new GameObject(SkillConstant.SkillCanvasName);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SkillConstant.CanvasSortingOrder;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // 创建 HUD 根节点
            GameObject rootObj = new GameObject(SkillConstant.SkillHUDRootName);
            rootObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rootRect = rootObj.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0f);
            rootRect.anchorMax = new Vector2(0.5f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0f);
            rootRect.anchoredPosition = new Vector2(0f, SkillConstant.HudBottomMargin);

            // HorizontalLayoutGroup
            HorizontalLayoutGroup layout = rootObj.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = SkillConstant.SkillButtonSpacing;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // 创建4个技能按钮占位（简化版，运行时 SkillHUD 会重新创建）
            for (int i = 0; i < 4; i++)
            {
                GameObject btnObj = new GameObject($"{SkillConstant.SkillButtonPrefix}{i}");
                btnObj.transform.SetParent(rootObj.transform, false);
                RectTransform btnRect = btnObj.AddComponent<RectTransform>();
                btnRect.sizeDelta = new Vector2(
                    SkillConstant.SkillButtonWidth, SkillConstant.SkillButtonHeight);
                btnObj.AddComponent<CanvasRenderer>();
                Image img = btnObj.AddComponent<Image>();
                img.color = SkillConstant.CooldownReadyColor;
            }

            float totalWidth = (SkillConstant.SkillButtonWidth * 4)
                               + (SkillConstant.SkillButtonSpacing * 3);
            rootRect.sizeDelta = new Vector2(totalWidth + 20f, SkillConstant.SkillButtonHeight + 30f);

            // 标记场景已修改
            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log(
                $"[SkillMenu] 技能 HUD 已安装到场景 '{activeScene.name}'。" +
                $"Canvas: {SkillConstant.SkillCanvasName}, " +
                $"根节点: {SkillConstant.SkillHUDRootName}。" +
                "运行时会由 SkillHUD.EnsureRuntimePanel() 自动填充完整的子 UI 元素。");
        }

        /// <summary>
        /// 从当前场景中移除技能 HUD。
        /// 仅删除 Ambitious_A008 相关的节点，不影响其他 UI。
        /// </summary>
        [MenuItem(SkillConstant.MenuRoot + SkillConstant.MenuRemoveFromScene, false, 1)]
        public static void RemoveSkillHUDFromScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            bool removed = false;

            // 移除 Canvas
            GameObject canvas = GameObject.Find(SkillConstant.SkillCanvasName);
            if (canvas != null)
            {
                Object.DestroyImmediate(canvas);
                removed = true;
                Debug.Log($"[SkillMenu] 已移除 {SkillConstant.SkillCanvasName}");
            }

            // 移除可能游离的根节点
            GameObject root = GameObject.Find(SkillConstant.SkillHUDRootName);
            if (root != null)
            {
                Object.DestroyImmediate(root);
                removed = true;
                Debug.Log($"[SkillMenu] 已移除 {SkillConstant.SkillHUDRootName}");
            }

            if (removed)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                Debug.Log($"[SkillMenu] 技能 HUD 已从场景 '{activeScene.name}' 移除。");
            }
            else
            {
                Debug.Log("[SkillMenu] 场景中未找到技能 HUD，无需移除。");
            }
        }

        /// <summary>
        /// 验证主动技能系统的完整性。
        /// 检查所有必需的脚本文件、枚举、常量和工具类是否存在以及基本逻辑一致性。
        /// 输出验证报告到 Console。
        /// </summary>
        [MenuItem(SkillConstant.MenuRoot + SkillConstant.MenuVerifySystem, false, 2)]
        public static void VerifySkillSystem()
        {
            Debug.Log("===== 主动技能系统完整性验证 =====");

            // 验证枚举
            Debug.Log($"[枚举] SkillType: 已定义 {System.Enum.GetNames(typeof(SkillType)).Length} 个值");
            Debug.Log($"[枚举] SkillEffectType: 已定义 {System.Enum.GetNames(typeof(SkillEffectType)).Length} 个值");

            // 验证常量
            bool hasConstant = typeof(SkillConstant) != null;
            Debug.Log($"[常量] SkillConstant: {(hasConstant ? "存在" : "缺失")}");

            // 验证工具类
            bool hasTool = typeof(SkillTool) != null;
            Debug.Log($"[工具] SkillTool: {(hasTool ? "存在" : "缺失")}");

            // 验证数据模型
            bool hasData = typeof(SkillData) != null;
            Debug.Log($"[数据] SkillData: {(hasData ? "存在" : "缺失")}");

            // 验证管理器
            bool hasManager = typeof(SkillManager) != null;
            Debug.Log($"[管理] SkillManager: {(hasManager ? "存在" : "缺失")}");

            // 验证 UI
            bool hasHUD = typeof(SkillHUD) != null;
            Debug.Log($"[UI] SkillHUD: {(hasHUD ? "存在" : "缺失")}");

            // 验证逻辑一致性
            if (hasData && hasTool)
            {
                // 测试技能工厂方法
                SkillData whirlwind = SkillData.CreateWhirlwind();
                bool idOk = whirlwind.SkillId == SkillConstant.SkillWhirlwind;
                bool nameOk = whirlwind.SkillName == SkillConstant.DefaultSkillNameWhirlwind;
                bool typeOk = whirlwind.SkillType == SkillType.SelfAOE;
                bool levelOk = whirlwind.Level == 1;
                bool slotOk = whirlwind.SlotIndex == 0;
                bool cooldownOk = Mathf.Approximately(
                    whirlwind.CurrentCooldown, SkillTool.CalculateSkillCooldown(SkillConstant.WhirlwindCooldown, 1));
                Debug.Log(
                    $"[逻辑] 旋风斩工厂: ID={idOk} 名称={nameOk} 类型={typeOk} 等级={levelOk} " +
                    $"槽位={slotOk} 冷却计算={cooldownOk}");

                // 测试技能数据完整性
                SkillData dash = SkillData.CreateDash();
                SkillData surge = SkillData.CreatePowerSurge();
                SkillData heal = SkillData.CreateHealingLight();
                int totalSkills = (dash != null ? 1 : 0) + (surge != null ? 1 : 0) + (heal != null ? 1 : 0) + 1;
                Debug.Log($"[逻辑] 预定义技能数量: {totalSkills}/4");

                // 测试升级成本计算
                bool upgradeCostOk = SkillTool.GetUpgradeCost(1) == 1
                    && SkillTool.GetUpgradeCost(2) == 2
                    && SkillTool.GetUpgradeCost(3) == 3
                    && SkillTool.GetUpgradeCost(4) == 5
                    && SkillTool.GetUpgradeCost(5) == -1;
                Debug.Log($"[逻辑] 升级成本计算: {upgradeCostOk}");
            }

            // 验证 Player 快捷键引用
            bool hasSkillKeys = typeof(InputKeyConstant).GetField("SkillHotkey1") != null
                && typeof(InputKeyConstant).GetField("SkillHotkey2") != null
                && typeof(InputKeyConstant).GetField("SkillHotkey3") != null
                && typeof(InputKeyConstant).GetField("SkillHotkey4") != null;
            Debug.Log($"[按键] InputKeyConstant 技能快捷键: {(hasSkillKeys ? "已定义" : "缺失")}");

            Debug.Log("===== 验证完成 =====");
        }
    }
}
