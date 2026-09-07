namespace LAB2D.Editor
{
    using LAB2D;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Editor 菜单工具：山门核心（MountainGateManager）调试。
    /// 用于验证 M1.3 胜负闸门：降级惩罚 / 终局失败 / 阶段胜利。
    /// 菜单路径：工具 > 山门 > ...
    /// </summary>
    public static class MountainGateMenu
    {
        private const string MenuRoot = "工具/山门/";

        [MenuItem(MenuRoot + "核心状态", false, 100)]
        private static void ShowCoreStatus()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Mountain Gate", "请在 Play Mode 中使用此功能。", "OK");
                return;
            }

            Gameplay.MountainGateManager gate = Gameplay.MountainGateManager.Instance;
            string statusText = string.Format(
                "核心位置: {0}\n" +
                "耐久: {1:F0}/{2:F0}\n" +
                "等级: {3}/{4}\n" +
                "被击破次数: {5}（P5 软化后不再终局）\n" +
                "终局失败: {6}\n" +
                "阶段胜利: {7}",
                gate.IsCorePlaced ? $"({gate.CorePosition.x}, {gate.CorePosition.y})" : "未放置",
                gate.CoreHp,
                (float)LAB2D.Domain.Gameplay.BuildingDamageRuleService.CoreMaxHp,
                gate.CoreLevel,
                LAB2D.Domain.Gameplay.BuildingDamageRuleService.CoreMaxLevel,
                gate.DownfallCount,
                gate.IsGameOver ? "是" : "否",
                gate.IsVictory ? "是" : "否");

            Debug.Log("<color=cyan>[MountainGate]</color>\n" + statusText);
            EditorUtility.DisplayDialog("Mountain Gate Status", statusText, "OK");
        }

        [MenuItem(MenuRoot + "模拟核心受击 100", false, 101)]
        private static void SimulateCoreDamage()
        {
            if (!RequirePlayMode())
            {
                return;
            }

            Gameplay.MountainGateManager.Instance.DamageCore(100f, null);
            Debug.Log("<color=cyan>[MountainGate]</color> 已模拟核心受击 100 点");
        }

        [MenuItem(MenuRoot + "升级核心（验证胜利）", false, 102)]
        private static void UpgradeCore()
        {
            if (!RequirePlayMode())
            {
                return;
            }

            // 升级有金币门槛（1→2 扣 200、2→3 扣 500）：调试菜单先补足本次消耗，验证胜利路径不被钱包卡住
            Gameplay.MountainGateManager gate = Gameplay.MountainGateManager.Instance;
            int cost = new LAB2D.Domain.Gameplay.BuildingDamageRuleService().GetCoreUpgradeCost(gate.CoreLevel);
            if (cost > 0)
            {
                Core.ServiceLocator.Get<Gameplay.CurrencyManager>()?.AddPlayerGold(cost);
            }

            bool ok = gate.TryUpgradeCore();
            Debug.Log($"<color=cyan>[MountainGate]</color> 升级核心：{(ok ? "成功" : "失败（未放置/已终局/已满级/金币不足）")}");
        }

        [MenuItem(MenuRoot + "连破3次（验证不再终局）", false, 103)]
        private static void BreakCoreThrice()
        {
            if (!RequirePlayMode())
            {
                return;
            }

            // P5 胜负软化：山门被破不再终局——连打 3 次致死伤害，观察惩罚+复活+游戏继续
            Gameplay.MountainGateManager gate = Gameplay.MountainGateManager.Instance;
            for (int i = 0; i < 3; i++)
            {
                gate.DamageCore(LAB2D.Domain.Gameplay.BuildingDamageRuleService.CoreMaxHp, null);
            }

            Debug.Log($"<color=cyan>[MountainGate]</color> 连破 3 次完成：被破={gate.DownfallCount} hp={gate.CoreHp:F0} 终局={(gate.IsGameOver ? "是（异常！）" : "否（已软化）")}");
        }

        private static bool RequirePlayMode()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Mountain Gate", "请在 Play Mode 中使用此功能。", "OK");
                return false;
            }

            return true;
        }
    }
}
