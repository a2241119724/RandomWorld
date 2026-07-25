namespace LAB2D.UI.Panel
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Constant;
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
            ServiceLocator.Register((BP)(object)this);
            this.Controller = ServiceLocator.Get<PanelController>();
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
                this.Panel = ServiceLocator.Get<ResourceManager>().Instantiate(this.Name, ServiceLocator.Get<PanelController>().Parent, false);
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
            AWorkerTask.LogProvider("Enter: " + this.GetType().Name, LogManager.LogLevelEnum.Trace);
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
            AWorkerTask.LogProvider("Exit: " + this.GetType().Name, LogManager.LogLevelEnum.Trace);
            this.Panel.SetActive(false);
        }

        /// <inheritdoc/>
        public virtual void OnClick_Back()
        {
            // 没有返回按钮的面板,显示暂停菜单
            ServiceLocator.Get<PanelController>().Show(PauseMenuPanel.Instance);
        }
    }
}
