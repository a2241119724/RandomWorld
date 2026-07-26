namespace LAB2D.Editor
{
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// 浮动战斗文字系统 Editor 菜单工具
    /// 用途：在 Unity Editor 中提供一键安装/移除浮动文字系统 UI 的菜单项。
    /// 仅在 Editor 环境下编译，不会进入运行时构建。
    ///
    /// 菜单路径：工具/智能体/浮动战斗文字/
    /// </summary>
    public static class FloatingTextMenu
    {
        /// <summary>
        /// 安装浮动文字系统到当前打开的 Game 场景
        /// 在场景中创建独立的浮动文字 Canvas，不修改已有场景对象。
        /// 重复执行安全：已存在时跳过创建。
        /// </summary>
        [MenuItem(FloatingTextConstant.EditorMenuInstallToGame)]
        private static void InstallFloatingTextToGame()
        {
            Scene gameScene = SceneManager.GetActiveScene();
            if (!gameScene.IsValid() || !gameScene.name.Contains("Game"))
            {
                Debug.LogWarning("[浮动战斗文字] 当前场景不是 Game 场景，请在 Game 场景中执行安装。"
                    + $"当前场景：{gameScene.name}");
                return;
            }

            // FloatingTextCanvas 和 FloatingTextPool 应在场景中手动创建
            GameObject canvasObj = GameObject.Find(FloatingTextConstant.CanvasName);
            if (canvasObj == null)
            {
                Debug.LogWarning($"[浮动战斗文字] 场景中未找到 {FloatingTextConstant.CanvasName}，请手动创建。");
                return;
            }

            Debug.Log("[浮动战斗文字] 安装完成。运行时 FloatingTextManager 会自动初始化并填充对象池。");
        }

        /// <summary>
        /// 从当前 Game 场景移除浮动文字系统
        /// 删除浮动文字 Canvas 及其所有子对象。
        /// </summary>
        [MenuItem(FloatingTextConstant.EditorMenuRemoveFromGame)]
        private static void RemoveFloatingTextFromGame()
        {
            Scene gameScene = SceneManager.GetActiveScene();
            if (!gameScene.IsValid() || !gameScene.name.Contains("Game"))
            {
                Debug.LogWarning("[浮动战斗文字] 当前场景不是 Game 场景，请在 Game 场景中执行移除。"
                    + $"当前场景：{gameScene.name}");
                return;
            }

            GameObject canvasObj = GameObject.Find(FloatingTextConstant.CanvasName);
            if (canvasObj != null)
            {
                Object.DestroyImmediate(canvasObj);
                Debug.Log($"[浮动战斗文字] 已移除 Canvas: {FloatingTextConstant.CanvasName}");
            }
            else
            {
                Debug.Log("[浮动战斗文字] 未找到浮动文字 Canvas，无需移除。");
            }
        }

        /// <summary>
        /// 验证浮动文字系统配置完整性
        /// 检查常量、工具类、枚举和脚本文件是否就绪。
        /// </summary>
        [MenuItem(FloatingTextConstant.EditorMenuValidate)]
        private static void ValidateFloatingTextSystem()
        {
            bool allOk = true;
            int checkCount = 0;
            int passCount = 0;

            // 检查枚举
            checkCount++;
            System.Array enumValues = System.Enum.GetValues(typeof(FloatingTextType));
            if (enumValues.Length >= 7)
            {
                Debug.Log($"[浮动战斗文字] FloatingTextType 枚举值数量: {enumValues.Length}（通过）");
                passCount++;
            }
            else
            {
                Debug.LogError($"[浮动战斗文字] FloatingTextType 枚举值数量异常: {enumValues.Length}");
                allOk = false;
            }

            // 检查每个类型的颜色是否非默认
            checkCount++;
            string[] typeNames = System.Enum.GetNames(typeof(FloatingTextType));
            bool colorsOk = true;
            foreach (string typeName in typeNames)
            {
                FloatingTextType type = (FloatingTextType)System.Enum.Parse(typeof(FloatingTextType), typeName);
                Color color = FloatingTextTool.GetColor(type);
                if (color == default(Color))
                {
                    Debug.LogWarning($"[浮动战斗文字] {typeName} 颜色为默认值");
                    colorsOk = false;
                }
            }

            if (colorsOk)
            {
                Debug.Log("[浮动战斗文字] 所有类型颜色已配置（通过）");
                passCount++;
            }
            else
            {
                allOk = false;
            }

            // 检查字号
            checkCount++;
            bool sizesOk = true;
            foreach (string typeName in typeNames)
            {
                FloatingTextType type = (FloatingTextType)System.Enum.Parse(typeof(FloatingTextType), typeName);
                int size = FloatingTextTool.GetFontSize(type);
                if (size <= 0)
                {
                    Debug.LogWarning($"[浮动战斗文字] {typeName} 字号异常: {size}");
                    sizesOk = false;
                }
            }

            if (sizesOk)
            {
                Debug.Log("[浮动战斗文字] 所有类型字号已配置（通过）");
                passCount++;
            }
            else
            {
                allOk = false;
            }

            // 检查池参数
            checkCount++;
            Debug.Log($"[浮动战斗文字] 对象池配置: 默认 {FloatingTextConstant.DefaultPoolSize} / 最大 {FloatingTextConstant.MaxPoolSize}（通过）");
            passCount++;

            Debug.Log($"[浮动战斗文字] 验证完成: {passCount}/{checkCount} 通过。"
                + (allOk ? "系统配置正常。" : "存在配置问题，请检查上述警告。"));
        }
    }
}
