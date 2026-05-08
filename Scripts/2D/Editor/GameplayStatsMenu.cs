namespace LAB2D
{
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Editor 菜单工具：查看当前运行时会话统计数据。
    /// 仅在 Unity Editor 中可用，用于验证 GameplaySessionStats 是否正常接入。
    /// 菜单路径：工具 > 玩法统计 > 查看会话统计
    /// </summary>
    public static class GameplayStatsMenu
    {
        private const string MenuRoot = "工具/玩法统计/";

        [MenuItem(MenuRoot + "查看会话统计")]
        private static void ShowSessionStats()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Gameplay Stats",
                    "请在 Play Mode 中使用此功能。\n统计数据仅在运行时生成。",
                    "OK");
                return;
            }

            GameplaySessionStats stats = GameplaySessionStats.Instance;
            if (stats == null)
            {
                EditorUtility.DisplayDialog(
                    "Gameplay Stats",
                    "GameplaySessionStats 实例未初始化。",
                    "OK");
                return;
            }

            string summary = stats.BuildSummaryText();
            Debug.Log("<color=cyan>[GameplayStats]</color>\n" + summary);
            EditorUtility.DisplayDialog(
                "Gameplay Session Stats",
                summary,
                "OK");
        }

        [MenuItem(MenuRoot + "重置会话统计")]
        private static void ResetSessionStats()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Gameplay Stats",
                    "请在 Play Mode 中使用此功能。",
                    "OK");
                return;
            }

            GameplaySessionStats stats = GameplaySessionStats.Instance;
            if (stats == null)
            {
                return;
            }

            stats.ResetSession();
            Debug.Log("<color=cyan>[GameplayStats]</color> 会话统计已重置。");
        }
    }
}
