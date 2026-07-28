namespace LAB2D.Editor
{
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// A004 波次 Boss 与奖励 Editor 菜单。
    /// 提供运行时调试和奖励模拟。
    /// </summary>
    public static class WaveBossRewardMenu
    {
        /// <summary>
        /// 查看运行时状态。
        /// </summary>
        [MenuItem(WaveBossRewardConstant.MenuRoot + "查看当前状态", false, 120)]
        private static void ShowCurrentState()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("波次Boss奖励", "请在 Play Mode 中查看运行时状态。", "确定");
                return;
            }

            string summary = WaveBossRewardManager.Instance.CurrentState.ToSummaryText();
            Debug.Log("<color=cyan>[A004 波次Boss奖励]</color>\n" + summary);
            EditorUtility.DisplayDialog("波次Boss奖励", summary, "确定");
        }

        /// <summary>
        /// 启用系统。
        /// </summary>
        [MenuItem(WaveBossRewardConstant.MenuRoot + "启用系统", false, 121)]
        private static void EnableSystem()
        {
            WaveBossRewardManager.Instance.Enable();
            EditorUtility.DisplayDialog("波次Boss奖励", "A004 系统已启用。", "确定");
        }

        /// <summary>
        /// 禁用系统。
        /// </summary>
        [MenuItem(WaveBossRewardConstant.MenuRoot + "禁用系统", false, 122)]
        private static void DisableSystem()
        {
            WaveBossRewardManager.Instance.Disable();
            EditorUtility.DisplayDialog("波次Boss奖励", "A004 系统已禁用，奖励 Buff 已清空。", "确定");
        }

        /// <summary>
        /// 启用提示。
        /// </summary>
        [MenuItem(WaveBossRewardConstant.MenuRoot + "启用提示", false, 123)]
        private static void EnableTip()
        {
            WaveBossRewardManager.Instance.SetTipEnabled(true);
            EditorUtility.DisplayDialog("波次Boss奖励", "Tip 提示已启用。", "确定");
        }

        /// <summary>
        /// 禁用提示。
        /// </summary>
        [MenuItem(WaveBossRewardConstant.MenuRoot + "禁用提示", false, 124)]
        private static void DisableTip()
        {
            WaveBossRewardManager.Instance.SetTipEnabled(false);
            EditorUtility.DisplayDialog("波次Boss奖励", "Tip 提示已禁用。", "确定");
        }

        /// <summary>
        /// 模拟普通波奖励。
        /// </summary>
        [MenuItem(WaveBossRewardConstant.MenuRoot + "调试/生成普通波奖励", false, 125)]
        private static void DebugCreateNormalReward()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("波次Boss奖励", "请在 Play Mode 中生成调试奖励。", "确定");
                return;
            }

            WaveBossRewardPanel.EnsureRuntimePanel();
            WaveBossRewardManager.Instance.CreateDebugRewardOptions(false);
        }

        /// <summary>
        /// 模拟 Boss 波奖励。
        /// </summary>
        [MenuItem(WaveBossRewardConstant.MenuRoot + "调试/生成Boss波奖励", false, 126)]
        private static void DebugCreateBossReward()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("波次Boss奖励", "请在 Play Mode 中生成调试奖励。", "确定");
                return;
            }

            WaveBossRewardPanel.EnsureRuntimePanel();
            WaveBossRewardManager.Instance.CreateDebugRewardOptions(true);
        }
    }
}
