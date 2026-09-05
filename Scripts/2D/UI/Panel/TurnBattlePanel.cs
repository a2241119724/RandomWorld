namespace LAB2D.UI.Panel
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.Gameplay.TurnBattle;
    using LAB2D.Domain.TurnBattle;
    using LAB2D.UI.Panel.PanelUI;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 回合制战斗面板 — B 键加入大世界交战后经 TurnBattleManager.BattleStarted 事件打开。
    /// 非覆盖面板推栈自动冻结大世界（Foreground.OnPause timeScale=0），Close 后自动恢复。
    /// 面板节点已在场景搭建（Init 场景绑定），战斗内容 UI 由 TurnBattleUI 构建；
    /// 回合流转全部经 Manager 事件：AwaitingPlayerAction 启用菜单，TurnResolved 演出，
    /// BattleFinished 演出结束横幅后关闭面板。
    /// </summary>
    public class TurnBattlePanel : ABasePanel<TurnBattlePanel>
    {
        private TurnBattleUI battleUI;

        public TurnBattlePanel()
        {
            this.Name = "TurnBattlePanel";
            this.Init(); // 面板节点已在场景搭建，走标准场景绑定（同 CultivationPanel 惯例），无代码创建兜底

            this.BindUI();
            this.BindManagerEvents();
        }

        private void BindUI()
        {
            if (this.Panel == null)
            {
                return;
            }

            this.battleUI = this.Panel.GetComponent<TurnBattleUI>();
            if (this.battleUI == null)
            {
                this.battleUI = this.Panel.AddComponent<TurnBattleUI>();
            }
        }

        /// <summary>Manager 事件接线 — 面板生命周期与战斗生命周期同步（构造时订阅，单例常驻）。</summary>
        private void BindManagerEvents()
        {
            TurnBattleManager manager = TurnBattleManager.Instance;
            manager.BattleStarted += this.OnBattleStarted;
            manager.AwaitingPlayerAction += this.OnAwaitingPlayerAction;
            manager.TurnResolved += this.OnTurnResolved;
            manager.BattleFinished += this.OnBattleFinished;
        }

        private void OnBattleStarted(TurnBattleState state)
        {
            // 被 Foreground 之外的模态面板盖住时开战属于异常路径（B 键入口已挡），作废战斗防写回错乱。
            // 判据用 IsForeground 而非 Panels.Count：ForegroundPanel 常驻栈底代表正常游玩态，
            // Count>0 会在正常开战时误杀成 AbortBattle（与 ProcessJoinBattle 同源问题）
            PanelController controller = ServiceLocator.Get<PanelController>();
            if (controller != null && !controller.IsForeground())
            {
                TurnBattleManager.Instance.AbortBattle();
                return;
            }

            this.Controller.Show(this);
            this.battleUI.BindBattle(state);
        }

        private void OnAwaitingPlayerAction(TurnBattleState state)
        {
            this.battleUI.OnAwaitPlayerAction(state);
        }

        private void OnTurnResolved(IReadOnlyList<TurnBattleActionResolution> resolutions)
        {
            this.battleUI.PlayResolutions(resolutions);
        }

        private void OnBattleFinished(TurnBattleResult result)
        {
            this.battleUI.PlayBattleFinished(result, this.CloseBattle);
        }

        private void CloseBattle()
        {
            if (this.Controller != null
                && this.Controller.Panels.Count > 0
                && this.Controller.Panels.Peek() == this)
            {
                this.Controller.Close();
            }
        }

        /// <summary>
        /// ESC — 战斗内按层次取消（点选目标 → 子菜单 → 逃跑确认框），
        /// 不直接关面板（免费退出破坏逃跑规则）。
        /// </summary>
        public override void OnClick_Back()
        {
            this.battleUI.HandleBack();
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
            this.battleUI.ClearBattle();
        }
    }
}
