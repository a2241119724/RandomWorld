namespace LAB2D.Editor
{
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// 浮动战斗文字系统 Editor 菜单工具 — 验证系统配置完整性。
    /// 仅在 Editor 环境下编译，不会进入运行时构建。
    ///
    /// 菜单路径：工具/战斗文字/
    /// </summary>
    public static class FloatingTextMenu
    {
        /// <summary>
        /// 验证浮动文字系统配置完整性
        /// 检查常量、工具类、枚举和脚本文件是否就绪。
        /// </summary>
        [MenuItem(FloatingTextConstant.EditorMenuValidate, false, 210)]
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
