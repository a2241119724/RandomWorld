namespace LAB2D.Editor
{
    using LAB2D;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Editor 菜单工具：查看和调试波次事件反馈系统状态。
    /// 仅在 Unity Editor 中可用，用于验证 WaveEventFeedback 是否正常工作。
    /// 菜单路径：工具 > 波次事件反馈 >
    ///
    /// 接入方式：无需任何配置，在 Unity Editor 中直接使用菜单项。
    /// 风险边界：仅读取运行时状态，不修改任何游戏数据或资源。
    /// </summary>
    public static class WaveEventFeedbackMenu
    {
        private const string MenuRoot = "工具/波次/事件反馈/";

        [MenuItem(MenuRoot + "查看反馈状态")]
        private static void ShowFeedbackStatus()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Wave Event Feedback",
                    "请在 Play Mode 中使用此功能。\n波次事件反馈数据仅在运行时生成。",
                    "确定");
                return;
            }

            WaveEventFeedback feedback = WaveEventFeedback.Instance;
            if (feedback == null)
            {
                EditorUtility.DisplayDialog(
                    "Wave Event Feedback",
                    "WaveEventFeedback 实例未初始化。",
                    "确定");
                return;
            }

            WaveFeedbackState state = feedback.CurrentState;
            string info;
            if (state == null)
            {
                info = "状态数据为空。";
            }
            else
            {
                info = state.ToSummaryText();
            }

            Debug.Log("<color=cyan>[WaveEventFeedback]</color>\n" + info);
            EditorUtility.DisplayDialog("Wave Event Feedback Status", info, "确定");
        }

        [MenuItem(MenuRoot + "启用反馈")]
        private static void EnableFeedback()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Wave Event Feedback",
                    "请在 Play Mode 中使用此功能。",
                    "确定");
                return;
            }

            WaveEventFeedback.Instance.Enable();
            EditorUtility.DisplayDialog(
                "Wave Event Feedback",
                "波次事件反馈已启用。\n现在波次开始/结束/清空/休息时会有 Tip 提示。",
                "确定");
        }

        [MenuItem(MenuRoot + "禁用反馈")]
        private static void DisableFeedback()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Wave Event Feedback",
                    "请在 Play Mode 中使用此功能。",
                    "确定");
                return;
            }

            WaveEventFeedback.Instance.Disable();
            EditorUtility.DisplayDialog(
                "Wave Event Feedback",
                "波次事件反馈已禁用。\n波次事件将不再产生 Tip 提示。",
                "确定");
        }

        [MenuItem(MenuRoot + "查看波次运行状态")]
        private static void ShowWaveManagerRuntimeState()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Wave Event Feedback",
                    "请在 Play Mode 中使用此功能。",
                    "确定");
                return;
            }

            WaveManager wm = WaveManager.Instance;
            if (wm == null)
            {
                EditorUtility.DisplayDialog(
                    "Wave Event Feedback",
                    "WaveManager 实例未初始化。",
                    "确定");
                return;
            }

            // 事件订阅状态只能从 WaveManager 内部查询，外部无法访问 C# event 的委托列表。
            // 这里通过 WaveEventFeedback 的启用状态间接反映事件订阅情况。
            WaveEventFeedback feedback = WaveEventFeedback.Instance;
            bool feedbackEnabled = feedback != null && feedback.CurrentState.feedbackEnabled;

            string info = "WaveManager 运行时状态:\n\n";
            info += $"波次反馈已订阅事件: {(feedbackEnabled ? "是" : "否")}\n";
            info += "\n";
            info += $"当前波次: {wm.CurrentWaveIndex}\n";
            info += $"已完成波次: {wm.TotalWavesCompleted}\n";
            info += $"波次进行中: {wm.IsWaveActive}\n";
            info += $"波间休息中: {wm.IsResting}\n";
            info += $"存活敌人: {wm.EnemiesAliveInWave}\n";
            info += $"难度倍率: {wm.CurrentDifficultyScale:F2}x";

            Debug.Log("<color=cyan>[WaveManager State]</color>\n" + info);
            EditorUtility.DisplayDialog("WaveManager Runtime State", info, "确定");
        }

        [MenuItem(MenuRoot + "模拟波次提示（测试）")]
        private static void SimulateWaveTip()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Wave Event Feedback",
                    "请在 Play Mode 中使用此功能。",
                    "确定");
                return;
            }

            // 使用 GlobalInit.ShowTip 模拟显示波次提示，验证 Tip 系统可用性
            try
            {
                if (GlobalInit.Instance != null)
                {
                    GlobalInit.Instance.ShowTip("[测试] 第 3 波来袭! 准备迎战!");
                    EditorUtility.DisplayDialog(
                        "Wave Event Feedback",
                        "已发送测试 Tip 提示。\n请在游戏画面中查看提示效果。",
                        "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        "Wave Event Feedback",
                        "GlobalInit 实例不存在，无法显示 Tip。\n请确保场景中已挂载 GlobalInit。",
                        "确定");
                }
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog(
                    "Wave Event Feedback",
                    $"Tip 测试失败: {e.Message}",
                    "确定");
            }
        }
    }
}
