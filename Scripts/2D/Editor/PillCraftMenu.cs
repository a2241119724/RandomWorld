namespace LAB2D.Editor
{
    using LAB2D.Domain.Gameplay.Alchemy;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// 炼丹开发菜单 — Play 模式下为玩家炼丹（品质 roll 走 PillRuleService 真随机）。
    /// 正式入口待 K 面板丹药按钮行接入（面板场景摆放，需摆节点后代码按名绑定）。
    /// 菜单: 工具 > 炼丹
    /// </summary>
    public static class PillCraftMenu
    {
        [MenuItem("工具/炼丹/回气散(聚气·练气)")]
        public static void CraftHuiQiSan()
        {
            Craft(PillLibrary.HuiQiSan.Id);
        }

        [MenuItem("工具/炼丹/培元丹(治伤·练气)")]
        public static void CraftPeiYuanDan()
        {
            Craft(PillLibrary.PeiYuanDan.Id);
        }

        [MenuItem("工具/炼丹/凝神丹(归元·筑基)")]
        public static void CraftNingShenDan()
        {
            Craft(PillLibrary.NingShenDan.Id);
        }

        [MenuItem("工具/炼丹/渡劫丹(破境辅助·金丹)")]
        public static void CraftDuJieDan()
        {
            Craft(PillLibrary.DuJieDan.Id);
        }

        [MenuItem("工具/炼丹/九转金丹(洗髓·元婴)")]
        public static void CraftJiuZhuanJinDan()
        {
            Craft(PillLibrary.JiuZhuanJinDan.Id);
        }

        private static void Craft(string pillId)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[PillMenu] 炼丹需 Play 模式（玩家在场）");
                return;
            }

            Gameplay.PillManager.Instance.TryCraftForPlayer(pillId);
        }
    }
}
