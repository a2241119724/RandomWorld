namespace LAB2D.Editor
{
    using LAB2D;
    using LAB2D.Domain.Gameplay.Faction;
    using LAB2D.Gameplay;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Editor 菜单工具：阵营系统（FactionManager）调试 — P1 验证主要抓手。
    /// 菜单路径：工具 > 阵营 > ...
    /// </summary>
    public static class FactionDebugMenu
    {
        private const string MenuRoot = "工具/阵营/";

        [MenuItem(MenuRoot + "阵营状态", false, 110)]
        private static void ShowFactionStatus()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Faction", "请在 Play Mode 中使用此功能。", "OK");
                return;
            }

            FactionManager manager = FactionManager.Instance;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"阵营数: {manager.Factions.Count}（玩家阵营: {(manager.PlayerFactionId == 0 ? "无" : manager.PlayerFactionId.ToString())}）");
            foreach (WorkerFaction faction in manager.Factions)
            {
                sb.AppendLine(
                    $"[{faction.FactionId}] {faction.Name} 领袖={faction.LeaderName ?? "空缺"} 成员={faction.MemberNames.Count} " +
                    $"金库={faction.TreasuryGold} 核心={(faction.Core.IsPlaced ? $"({faction.Core.X},{faction.Core.Y}) Lv{faction.Core.Level} hp={faction.Core.Hp:F0}" : "未放置")}");
            }

            if (manager.Factions.Count == 0)
            {
                sb.AppendLine("（暂无阵营——高野心 Worker 攒够 800 金后自立门户，或用下方菜单强制创建）");
            }

            Debug.Log("<color=cyan>[Faction]</color>\n" + sb);
            EditorUtility.DisplayDialog("Faction Status", sb.ToString(), "OK");
        }

        [MenuItem(MenuRoot + "玩家创建阵营（自动补足金币）", false, 111)]
        private static void CreatePlayerFaction()
        {
            if (!RequirePlayMode())
            {
                return;
            }

            Core.ServiceLocator.Get<CurrencyManager>()?.AddPlayerGold(FactionCreationRuleService.FoundingCostGold);
            bool ok = FactionManager.Instance.TryCreateFactionByPlayer();
            Debug.Log($"<color=cyan>[Faction]</color> 玩家创建阵营：{(ok ? "成功" : "失败（已有阵营/金币不足/核心无处放）")}");
        }

        [MenuItem(MenuRoot + "强制野心最高 Worker 建阵营", false, 112)]
        private static void ForceWorkerFound()
        {
            if (!RequirePlayMode())
            {
                return;
            }

            Character.Worker.AWorker best = null;
            AWorker.WorkerData bestWd = null;
            var wm = Core.ServiceLocator.Get<Character.Worker.WorkerManager>();
            foreach (Character.Worker.AWorker worker in wm.Characters)
            {
                AWorker.WorkerData wd = worker?.CharacterDataLAB as AWorker.WorkerData;
                if (wd == null || wd.FactionId != 0) continue;
                if (bestWd == null || wd.Personality.Ambition > bestWd.Personality.Ambition)
                {
                    best = worker;
                    bestWd = wd;
                }
            }

            if (best == null)
            {
                Debug.Log("<color=cyan>[Faction]</color> 无可造阵营的 Worker（全员已有阵营或无 Worker）");
                return;
            }

            // 造格：性格/阶段/钱包拉满，走真实 CanFound 闸门
            bestWd.LifeStage = Domain.Worker.WorkerLifeStage.Established;
            bestWd.Personality = new Domain.Worker.WorkerPersonality(
                bestWd.Personality.Mood, 85f, bestWd.Personality.Diligence, 75f);
            bestWd.Wallet = bestWd.Wallet + new Domain.Worker.CurrencyAmount(FactionCreationRuleService.FoundingCostGold);

            bool ok = FactionManager.Instance.TryCreateFactionByWorker(best);
            Debug.Log($"<color=cyan>[Faction]</color> 强制建阵营（{best.name}）：{(ok ? "成功" : "失败（名额满/选址失败也不阻断创建）")}");
        }

        [MenuItem(MenuRoot + "强制无阵营 Worker 入伙（第一个阵营）", false, 113)]
        private static void ForceJoin()
        {
            if (!RequirePlayMode())
            {
                return;
            }

            FactionManager manager = FactionManager.Instance;
            if (manager.Factions.Count == 0)
            {
                Debug.Log("<color=cyan>[Faction]</color> 无阵营可入");
                return;
            }

            WorkerFaction target = manager.Factions[0];
            var wm = Core.ServiceLocator.Get<Character.Worker.WorkerManager>();
            foreach (Character.Worker.AWorker worker in wm.Characters)
            {
                AWorker.WorkerData wd = worker?.CharacterDataLAB as AWorker.WorkerData;
                if (wd != null && wd.FactionId == 0)
                {
                    bool ok = manager.TryJoinFaction(worker, target);
                    Debug.Log($"<color=cyan>[Faction]</color> 强制入伙（{worker.name} → {target.Name}）：{(ok ? "成功" : "失败")}");
                    return;
                }
            }

            Debug.Log("<color=cyan>[Faction]</color> 无可入伙 Worker（全员已有阵营）");
        }

        [MenuItem(MenuRoot + "立即评估入伙+核心重试（跳过日更）", false, 114)]
        private static void ForceEvaluate()
        {
            if (!RequirePlayMode())
            {
                return;
            }

            FactionManager.Instance.ForceDailyEvaluation();
            Debug.Log("<color=cyan>[Faction]</color> 已执行入伙评估 + 核心选址重试（详见 [FactionDiag] 日志）");
        }

        [MenuItem(MenuRoot + "玩家退出阵营", false, 115)]
        private static void LeavePlayerFaction()
        {
            if (!RequirePlayMode())
            {
                return;
            }

            bool ok = FactionManager.Instance.TryLeaveFactionByPlayer();
            Debug.Log($"<color=cyan>[Faction]</color> 玩家退出阵营：{(ok ? "成功" : "失败（无阵营）")}");
        }

        [MenuItem(MenuRoot + "P2 生成阵营任务（跳过日更）", false, 120)]
        private static void ForceQuests()
        {
            if (!RequirePlayMode())
            {
                return;
            }

            FactionManager.Instance.ForceDailyQuests();
            Debug.Log("<color=cyan>[Faction]</color> 已生成阵营任务（核心附近资源，详见 [FactionDiag]/[TaskDiag] 日志）");
        }

        [MenuItem(MenuRoot + "P2 玩家捐献100G（自动补金币）", false, 121)]
        private static void PlayerDonate()
        {
            if (!RequirePlayMode())
            {
                return;
            }

            Core.ServiceLocator.Get<CurrencyManager>()?.AddPlayerGold(100);
            FactionManager manager = FactionManager.Instance;
            WorkerFaction faction = manager.GetFactionById(manager.PlayerFactionId);
            if (faction == null)
            {
                Debug.Log("<color=cyan>[Faction]</color> 玩家无阵营，无法捐献");
                return;
            }

            bool ok = manager.TryDonateGoldByPlayer(100);
            Debug.Log($"<color=cyan>[Faction]</color> 捐献100G：{(ok ? $"成功（金库={faction.TreasuryGold} 贡献={manager.GetContribution(faction.FactionId, FactionManager.PlayerAccountName):F0}）" : "失败")}");
        }

        [MenuItem(MenuRoot + "P2 玩家兑换25G", false, 122)]
        private static void PlayerExchange()
        {
            if (!RequirePlayMode())
            {
                return;
            }

            FactionManager manager = FactionManager.Instance;
            bool ok = manager.TryExchangeContributionByPlayer(25);
            float left = manager.GetContribution(manager.PlayerFactionId, FactionManager.PlayerAccountName);
            Debug.Log($"<color=cyan>[Faction]</color> 兑换25G（耗50贡献，日限50G）：{(ok ? $"成功（余贡献={left:F0}）" : $"失败（余贡献={left:F0}，需≥50）")}");
        }

        [MenuItem(MenuRoot + "P2 玩家升级核心", false, 123)]
        private static void PlayerUpgradeCore()
        {
            if (!RequirePlayMode())
            {
                return;
            }

            FactionManager manager = FactionManager.Instance;
            WorkerFaction faction = manager.GetFactionById(manager.PlayerFactionId);
            if (faction == null)
            {
                Debug.Log("<color=cyan>[Faction]</color> 玩家无阵营，无法升级");
                return;
            }

            bool ok = manager.TryUpgradeCoreByPlayer();
            Debug.Log($"<color=cyan>[Faction]</color> 升级核心：{(ok ? $"成功（Lv{faction.Core.Level} hp={faction.Core.Hp:F0}）" : "失败（满级/金库或贡献不足，见 Tip）")}");
        }

        [MenuItem(MenuRoot + "P4 重创阵营核心（第一个阵营 -300hp）", false, 140)]
        private static void DamageFirstCore()
        {
            if (!RequirePlayMode())
            {
                return;
            }

            FactionManager manager = FactionManager.Instance;
            WorkerFaction faction = null;
            foreach (WorkerFaction f in manager.Factions)
            {
                if (f.Core.IsPlaced)
                {
                    faction = f;
                    break;
                }
            }

            if (faction == null)
            {
                Debug.Log("<color=cyan>[Faction]</color> 无已放置核心的阵营");
                return;
            }

            var corePos = new Vector3Int(faction.Core.X, faction.Core.Y, 0);
            manager.DamageCoreAt(corePos, 300f, null);
            bool stillExists = manager.GetFactionById(faction.FactionId) != null;
            Debug.Log($"<color=cyan>[Faction]</color> 「{faction.Name}」核心受击 300 → hp={faction.Core.Hp:F0} 陷落次数={faction.Core.DownfallCount}（仍在={stillExists}）");
        }

        [MenuItem(MenuRoot + "P5 强制宣战（前两个阵营，免闸门）", false, 150)]
        private static void ForceDeclareWar()
        {
            if (!RequirePlayMode())
            {
                return;
            }

            FactionManager manager = FactionManager.Instance;
            if (manager.Factions.Count < 2)
            {
                Debug.Log("<color=cyan>[Faction]</color> 阵营不足 2 个，无法宣战");
                return;
            }

            WorkerFaction a = manager.Factions[0];
            WorkerFaction b = manager.Factions[1];
            bool ok = manager.DeclareWar(a.FactionId, b.FactionId, true);
            Debug.Log($"<color=cyan>[Faction]</color> 强制宣战 「{a.Name}」→「{b.Name}」：{(ok ? "成功（入夜将召回攻伐任务）" : "失败（已在交战）")}");
        }

        [MenuItem(MenuRoot + "P5 强制和谈（首个交战对）", false, 151)]
        private static void ForceMakePeace()
        {
            if (!RequirePlayMode())
            {
                return;
            }

            FactionManager manager = FactionManager.Instance;
            foreach ((int a, int b) in manager.WarPairs)
            {
                bool ok = manager.MakePeace(a, b);
                Debug.Log($"<color=cyan>[Faction]</color> 强制和谈 {a}↔{b}：{(ok ? "成功" : "失败")}");
                return;
            }

            Debug.Log("<color=cyan>[Faction]</color> 无交战对");
        }

        [MenuItem(MenuRoot + "P5 立即攻伐窗口（强制派兵）", false, 152)]
        private static void ForceRaidWindow()
        {
            if (!RequirePlayMode())
            {
                return;
            }

            FactionManager manager = FactionManager.Instance;
            if (manager.WarPairs.Count == 0)
            {
                Debug.Log("<color=cyan>[Faction]</color> 无交战对，先强制宣战");
                return;
            }

            int dispatched = manager.DispatchRaidWindows();
            Debug.Log($"<color=cyan>[Faction]</color> 攻伐窗口派兵：{dispatched} 人出征（闲散成员，详见 [FactionDiag]/[TaskDiag] 日志）");
        }

        [MenuItem(MenuRoot + "P3 立即选举（全部阵营）", false, 130)]
        private static void ForceElection()
        {
            if (!RequirePlayMode())
            {
                return;
            }

            FactionManager manager = FactionManager.Instance;
            if (manager.Factions.Count == 0)
            {
                Debug.Log("<color=cyan>[Faction]</color> 无阵营可选举");
                return;
            }

            var sb = new System.Text.StringBuilder("选举前领袖：\n");
            foreach (WorkerFaction faction in manager.Factions)
            {
                sb.AppendLine($"[{faction.FactionId}] {faction.Name} = {faction.LeaderName ?? "空缺"}");
            }

            manager.ForceElection();

            sb.AppendLine("选举后领袖：");
            foreach (WorkerFaction faction in manager.Factions)
            {
                sb.AppendLine($"[{faction.FactionId}] {faction.Name} = {faction.LeaderName ?? "空缺"}（下届第 {faction.NextElectionDay} 日）");
            }

            Debug.Log("<color=cyan>[Faction]</color>\n" + sb);
        }

        private static bool RequirePlayMode()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Faction", "请在 Play Mode 中使用此功能。", "OK");
                return false;
            }

            return true;
        }
    }
}
