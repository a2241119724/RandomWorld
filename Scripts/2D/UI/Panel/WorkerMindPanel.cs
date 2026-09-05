namespace LAB2D.UI.Panel
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Core;
    using LAB2D.UI.Panel.PanelUI;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Worker 心智面板 — 记忆流/关系网/信念四轴/对玩家意志/人格/修仙页可视化。
    /// 纯读面板（不改任何存档数据），IsOverlay=true 不暂停游戏，F12 切换（GlobalInputProcessor 分发）。
    /// UI 骨架由 Game.unity 场景摆放（WorkerMindUI 组件已挂 Panel 上），代码只绑定引用、不创建结构。
    /// </summary>
    public class WorkerMindPanel : ABasePanel<WorkerMindPanel>
    {
        private WorkerMindUI mindUI;

        public WorkerMindPanel()
        {
            this.Name = "WorkerMindPanel";
            this.Init();
            this.BindUI();
        }

        /// <inheritdoc/>
        public override bool IsOverlay => true;

        private void BindUI()
        {
            if (this.Panel == null) return;

            this.mindUI = this.Panel.GetComponent<WorkerMindUI>();
            if (this.mindUI == null)
            {
                AWorkerTask.LogProvider("WorkerMindPanel: 场景 Panel 上缺少 WorkerMindUI 组件", LogManager.LogLevelEnum.Warning);
            }

            Button closeBtn = this.Panel.transform.Find("CloseBtn")?.GetComponent<Button>();
            if (closeBtn != null)
            {
                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(() => this.Controller.Close());
            }
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            if (this.mindUI != null)
            {
                this.mindUI.RefreshAll();
            }
        }

        /// <inheritdoc/>
        public override void OnClick_Back()
        {
            this.Controller.Close();
        }
    }
}
