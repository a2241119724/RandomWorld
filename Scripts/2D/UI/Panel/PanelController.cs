namespace LAB2D
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 面板控制器
    /// </summary>
    public class PanelController : Singleton<PanelController>
    {
        public PanelController()
        {
            this.Parent = GameObject.FindGameObjectWithTag(TagConstant.UI_TAG).transform;
            this.Panels = new Stack<IBasePanel>();
            if (this.Panels == null)
            {
                LogManager.Instance.Log("panels assign resource Error!!!", LogManager.LogLevel.Error);
                return;
            }
        }

        /// <summary>
        /// 所有面板父物体
        /// </summary>
        public Transform Parent { get; set; }

        /// <summary>
        /// 面板栈
        /// </summary>
        public Stack<IBasePanel> Panels { get; set; }

        /// <summary>
        /// 展示下一个界面
        /// </summary>
        /// <param name="basePanel">下一个界面信息</param>
        public void Show(IBasePanel basePanel)
        {
            if (this.Panels.Count > 0 && !(basePanel is ItemInfoPanel
                || basePanel is AIChatPanel))
            {
                this.Panels.Peek().OnPause();
            }

            this.Panels.Push(basePanel);
            basePanel.OnEnter();
        }

        /// <summary>
        /// 关闭面板
        /// </summary>
        public void Close()
        {
            if (this.Panels.Count > 0)
            {
                // 先pop再执行退出
                IBasePanel panel = this.Panels.Pop();
                panel.OnExit();
            }

            if (this.Panels.Count > 0)
            {
                this.Panels.Peek().OnRun();
            }
        }

        /// <summary>
        /// 最上面的面板是否是前景面板
        /// </summary>
        /// <returns>是否</returns>
        public bool IsForeground()
        {
            if (this.Panels.Count > 0)
            {
                return this.Panels.Peek() == ForegroundPanel.Instance;
            }

            return false;
        }
    }
}