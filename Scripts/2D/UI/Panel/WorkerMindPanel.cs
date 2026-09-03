namespace LAB2D.UI.Panel
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.UI.Panel.PanelUI;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Worker 心智面板 — 记忆流/关系网/信念四轴/对玩家意志/人格/修仙页可视化。
    /// 纯读面板（不改任何存档数据），IsOverlay=true 不暂停游戏，F12 切换（GlobalInputProcessor 分发）。
    /// UI 全部由 WorkerMindUI 代码构建（Game.unity 无法手改 YAML，不走场景摆放/prefab）。
    /// </summary>
    public class WorkerMindPanel : ABasePanel<WorkerMindPanel>
    {
        private WorkerMindUI mindUI;

        public WorkerMindPanel()
        {
            this.Name = "WorkerMindPanel";

            // 安全加载：先找场景对象 → 最后创建空占位（无 prefab，结构由 WorkerMindUI 代码构建）
            Transform parent = this.Controller?.Parent;
            if (parent == null)
            {
                GameObject uiRoot = GameObject.FindGameObjectWithTag(Constant.TagConstant.UI_TAG);
                parent = uiRoot?.transform;
            }

            if (parent != null)
            {
                Transform existing = parent.Find(this.Name);
                if (existing != null)
                {
                    this.Panel = existing.gameObject;
                }
                else
                {
                    this.Panel = new GameObject(this.Name, typeof(RectTransform));
                    this.Panel.transform.SetParent(parent, false);
                }

                this.Panel.name = this.Name;
                this.Panel.SetActive(false);
            }

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
                this.mindUI = this.Panel.AddComponent<WorkerMindUI>();
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
