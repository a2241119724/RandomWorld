namespace LAB2D.Editor
{
    using LAB2D;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Editor 菜单工具：控制波次系统（WaveManager）。
    /// 仅在 Unity Editor 中可用，用于快速启停波次和查看状态。
    /// 菜单路径：工具 > 波次管理 > ...
    /// </summary>
    public static class WaveManagerMenu
    {
        private const string MenuRoot = "工具/波次/管理/";

        [MenuItem(MenuRoot + "开始波次")]
        private static void StartWaves()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Wave Manager",
                    "请在 Play Mode 中使用此功能。\n波次系统仅在运行时工作。",
                    "OK");
                return;
            }

            if (WaveManager.Instance == null)
            {
                EditorUtility.DisplayDialog(
                    "Wave Manager",
                    "WaveManager 实例未初始化。请检查 Singleton 配置。",
                    "OK");
                return;
            }

            WaveManager.Instance.StartWaves();
            Debug.Log("<color=cyan>[WaveManager]</color> 波次系统已启动！");
        }

        [MenuItem(MenuRoot + "停止波次")]
        private static void StopWaves()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Wave Manager",
                    "请在 Play Mode 中使用此功能。",
                    "OK");
                return;
            }

            if (WaveManager.Instance == null)
            {
                return;
            }

            WaveManager.Instance.StopWaves();
            Debug.Log("<color=cyan>[WaveManager]</color> 波次系统已停止，恢复默认生成模式。");
        }

        [MenuItem(MenuRoot + "查看波次状态")]
        private static void ShowWaveStatus()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Wave Manager",
                    "请在 Play Mode 中使用此功能。",
                    "OK");
                return;
            }

            if (WaveManager.Instance == null)
            {
                EditorUtility.DisplayDialog(
                    "Wave Manager",
                    "WaveManager 实例未初始化。",
                    "OK");
                return;
            }

            WaveSummary summary = WaveManager.Instance.GetWaveSummary();
            string statusText = string.Format(
                "当前波次: {0}\n" +
                "已完成波次: {1}\n" +
                "波次内已击杀: {2}\n" +
                "波次内存活敌人: {3}\n" +
                "难度缩放: {4:0.00}x\n" +
                "波次战斗中: {5}\n" +
                "波间休息中: {6}",
                summary.currentWaveIndex,
                summary.totalWavesCompleted,
                summary.enemiesDefeatedInWave,
                summary.enemiesAliveInWave,
                summary.difficultyScale,
                summary.isWaveActive ? "是" : "否",
                summary.isResting ? "是" : "否");

            Debug.Log("<color=cyan>[WaveManager]</color>\n" + statusText);
            EditorUtility.DisplayDialog("Wave Manager Status", statusText, "OK");
        }

        [MenuItem(MenuRoot + "快速查看配置")]
        private static void QuickConfigure()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Wave Manager",
                    "请在 Play Mode 中运行，然后使用此菜单修改运行时配置。\n" +
                    "或在代码中直接设置 WaveManager.Instance.Config 属性。",
                    "OK");
                return;
            }

            if (WaveManager.Instance == null)
            {
                return;
            }

            WaveConfig config = WaveManager.Instance.Config;
            string message = string.Format(
                "当前波次配置:\n" +
                "  基础敌人数: {0}\n" +
                "  每波增量: {1}\n" +
                "  波间休息: {2}s\n" +
                "  生成间隔: {3}s\n" +
                "  最大存活敌人: {4}\n" +
                "  总波次 (0=无限): {5}\n" +
                "  难度缩放/波: {6:0.00}x\n" +
                "  随机生成位置: {7}\n\n" +
                "如需修改，请在代码中设置 WaveManager.Instance.Config。",
                config.baseEnemyCount,
                config.enemiesPerWaveIncrease,
                config.restTimeBetweenWaves,
                config.spawnInterval,
                config.maxAliveEnemies,
                config.totalWaves,
                config.difficultyScalePerWave,
                config.useRandomSpawnPositions ? "是" : "否");

            EditorUtility.DisplayDialog("Wave Manager Config", message, "OK");
        }
    }
}
