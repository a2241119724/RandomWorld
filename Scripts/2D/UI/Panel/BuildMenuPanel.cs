namespace LAB2D
{
    using UnityEngine.UI;

    /// <summary>
    /// 建造菜单面板
    /// </summary>
    public class BuildMenuPanel : ABasePanel<BuildMenuPanel>
    {
        public BuildMenuPanel()
        {
            this.Name = "BuildMenu";
            this.Select = new SelectItemData();
            this.Init();
            Tool.GetComponentInChildren<Button>(this.Panel, "BackGame").onClick.AddListener(this.OnClick_Back);
            Tool.GetComponentInChildren<Button>(this.Panel, "StartBuild").onClick.AddListener(this.OnClick_StartBuild);
        }

        /// <summary>
        /// 选择的建造物品
        /// </summary>
        public SelectItemData Select { get; set; }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();

            // 进入游戏主界面
            this.Controller.Show(ForegroundPanel.Instance);
        }

        /// <summary>
        /// 关闭面板
        /// </summary>
        public override void OnClick_Back()
        {
            this.Controller.Close();
        }

        /// <summary>
        /// 开始建造
        /// </summary>
        public void OnClick_StartBuild()
        {
            // GameObject g = PrefabManager.Instance.getByAll(Select.itemData.itemName);
            // 关闭所有面板
            while (this.Controller.Panels.Count > 0)
            {
                this.Controller.Close();
            }

            BuildingUI.Instance.enabled = true;
        }
    }
}
