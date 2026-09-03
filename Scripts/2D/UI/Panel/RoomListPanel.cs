namespace LAB2D.UI.Panel
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.UI.Panel.PanelUI;
        using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 本地房间列表面板 — 显示 RoomManager 中所有建造的房间及其状态。
    /// 继承 ABasePanel，IsOverlay=true（不暂停游戏）。
    /// </summary>
    public class RoomListPanel : ABasePanel<RoomListPanel>
    {
        private RoomListUI roomListUI;

        public RoomListPanel()
        {
            this.Name = "RoomListPanel";

            // 安全加载：先找场景对象 → 再试 Prefab → 最后创建空占位
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
                    try { this.Panel = ServiceLocator.Get<ResourceManager>().Instantiate(this.Name, parent, false); }
                    catch (System.Exception ex) { AWorkerTask.LogProvider($"[UIDiag] RoomListPanel 构造时实例化 Prefab 失败（回退空面板）: {ex.Message}", LogManager.LogLevelEnum.Warning); }

                    if (this.Panel == null)
                    {
                        this.Panel = new GameObject(this.Name, typeof(RectTransform));
                        this.Panel.transform.SetParent(parent, false);
                    }
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

            this.roomListUI = this.Panel.GetComponent<RoomListUI>();
            if (this.roomListUI == null)
            {
                this.roomListUI = this.Panel.AddComponent<RoomListUI>();
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
            if (this.roomListUI != null)
            {
                this.roomListUI.RefreshRoomList();
            }
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
        }

        /// <inheritdoc/>
        public override void OnClick_Back()
        {
            this.Controller.Close();
        }
    }
}
