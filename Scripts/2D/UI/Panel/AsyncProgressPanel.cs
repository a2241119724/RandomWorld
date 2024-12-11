using UnityEngine;

namespace LAB2D {
    public class AsyncProgressPanel : BasePanel<AsyncProgressPanel>
    {
        public AsyncProgressPanel() {
            Name = "AsyncProgress";
            setPanel();
            panel.transform.GetComponent<AsyncProgressUI>().complete += complate;
        }

        public override void OnEnter()
        {
            base.OnEnter();
        }

        public override void OnExit()
        {
            base.OnExit();
            // 进入游戏主界面
            controller.show(ForegroundPanel.Instance);
        }

        private void complate() {
            // 关闭该界面
            controller.close();        
        } 
    }
}