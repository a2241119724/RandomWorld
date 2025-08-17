namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// 面板基类
    /// </summary>
    /// <typeparam name="BP">单例</typeparam>
    public abstract class ABasePanel<BP> : Singleton<BP>, IBasePanel
        where BP : new()
    {
        public ABasePanel()
        {
            this.Controller = PanelController.Instance;
        }

        /// <summary>
        /// 面板物体
        /// </summary>
        public GameObject Panel { get; set; }

        /// <summary>
        /// 通过Name获取对应的GameObject Panel
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 切换和关闭面板
        /// </summary>
        protected PanelController Controller { get; set; }

        /// <inheritdoc/>
        public void Init(string root = TagConstant.UI_TAG)
        {
            Transform t = GameObject.FindGameObjectWithTag(root).transform.Find(this.Name);
            if (t == null)
            {
                this.Panel = ResourceManager.Instance.Instantiate(this.Name, PanelController.Instance.Parent, false);
            }
            else
            {
                this.Panel = t.gameObject;
            }

            this.Panel.name = this.Name;
            this.Panel.SetActive(false);
        }

        /// <inheritdoc/>
        public virtual void OnEnter()
        {
            LogManager.Instance.Log("Enter: " + this.GetType().Name, LogManager.LogLevel.Info);
            if (this.Panel == null)
            {
                return;
            }

            this.Panel.SetActive(true);
        }

        /// <inheritdoc/>
        public virtual void OnPause()
        {
        }

        /// <inheritdoc/>
        public virtual void OnRun()
        {
        }

        /// <inheritdoc/>
        public virtual void OnExit()
        {
            LogManager.Instance.Log("Exit: " + this.GetType().Name, LogManager.LogLevel.Info);
            this.Panel.SetActive(false);
        }

        /// <inheritdoc/>
        public virtual void OnClick_Back()
        {
            // 没有返回按钮的面板,显示暂停菜单
            PanelController.Instance.Show(PauseMenuPanel.Instance);
        }
    }
}