namespace LAB2D.Editor
{
    using LAB2D;
    using System.Text;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Editor 菜单工具：查看连击增益系统运行时状态和等级配置表。
    /// 仅在 Unity Editor 中可用，用于调试和验证 ComboBonusManager 是否正常工作。
    /// 菜单路径：工具 > 连击增益 >
    /// </summary>
    public static class ComboBonusMenu
    {
        private const string MenuRoot = "工具/连击增益/";

        [MenuItem(MenuRoot + "查看连击状态", false, 500)]
        private static void ShowComboStatus()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Combo Bonus",
                    "请在 Play Mode 中使用此功能。\n连击增益数据仅在运行时生成。",
                    "OK");
                return;
            }

            ComboBonusManager mgr = ComboBonusManager.Instance;
            if (mgr == null)
            {
                EditorUtility.DisplayDialog(
                    "Combo Bonus",
                    "ComboBonusManager 实例未初始化。",
                    "OK");
                return;
            }

            string info = $"当前连击数: {mgr.CurrentCombo}\n" +
                          $"连击等级索引: {mgr.CurrentTierIndex}\n" +
                          $"伤害倍率: {mgr.DamageMultiplier:F2}x\n" +
                          $"经验倍率: {mgr.ExperienceMultiplier:F2}x\n" +
                          $"等级标签: {(string.IsNullOrEmpty(mgr.GetCurrentTierLabel()) ? "(无)" : mgr.GetCurrentTierLabel())}";

            Debug.Log("<color=cyan>[ComboBonus]</color>\n" + info);
            EditorUtility.DisplayDialog("Combo Bonus Status", info, "OK");
        }

        [MenuItem(MenuRoot + "查看全部等级", false, 501)]
        private static void ShowAllTiers()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("连击增益等级配置表:");
            sb.AppendLine("══════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine("  1-2:   伤害 1.00x  经验 1.0x  (无加成)");
            sb.AppendLine("  3-4:   伤害 1.15x  经验 1.3x  连击 x3!");
            sb.AppendLine("  5-9:   伤害 1.30x  经验 1.6x  连击 x5! 伤害提升!");
            sb.AppendLine(" 10-19:  伤害 1.50x  经验 2.0x  连击 x10! 激增!");
            sb.AppendLine(" 20-49:  伤害 1.80x  经验 3.0x  连击 x20! 无双!");
            sb.AppendLine(" 50+:    伤害 2.50x  经验 5.0x  连击 x50! 传说!");
            sb.AppendLine();
            sb.AppendLine("连击超时时间: 4 秒（由 GameplaySessionStats 控制）");
            sb.AppendLine("连击中断时显示提示并重置倍率。");

            Debug.Log("<color=cyan>[ComboBonus Tiers]</color>\n" + sb.ToString());
            EditorUtility.DisplayDialog("Combo Bonus Tiers", sb.ToString(), "OK");
        }

        [MenuItem(MenuRoot + "模拟查询（连击=3）", false, 502)]
        private static void SimulateCombo3()
        {
            float dmg = ComboBonusManager.GetDamageMultiplierForCombo(3);
            float exp = ComboBonusManager.GetExperienceMultiplierForCombo(3);
            EditorUtility.DisplayDialog(
                "Combo Query: 3",
                $"连击数 3:\n伤害倍率: {dmg:F2}x\n经验倍率: {exp:F2}x",
                "OK");
        }

        [MenuItem(MenuRoot + "模拟查询（连击=5）", false, 503)]
        private static void SimulateCombo5()
        {
            float dmg = ComboBonusManager.GetDamageMultiplierForCombo(5);
            float exp = ComboBonusManager.GetExperienceMultiplierForCombo(5);
            EditorUtility.DisplayDialog(
                "Combo Query: 5",
                $"连击数 5:\n伤害倍率: {dmg:F2}x\n经验倍率: {exp:F2}x",
                "OK");
        }

        [MenuItem(MenuRoot + "模拟查询（连击=50）", false, 504)]
        private static void SimulateCombo50()
        {
            float dmg = ComboBonusManager.GetDamageMultiplierForCombo(50);
            float exp = ComboBonusManager.GetExperienceMultiplierForCombo(50);
            EditorUtility.DisplayDialog(
                "Combo Query: 50",
                $"连击数 50:\n伤害倍率: {dmg:F2}x\n经验倍率: {exp:F2}x",
                "OK");
        }
    }
}
