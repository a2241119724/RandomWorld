using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class SettingMenuPanel : BasePanel<SettingMenuPanel>
    {
        private Camera[] cameras; // 2D与2.5D相机
        private Canvas rootCanvas; // UI root

        public SettingMenuPanel()
        {
            Name = "SettingMenu";
            setPanel();
            //cameras = Object.FindObjectsOfType(typeof(Camera), true);
            cameras = new Camera[2];
            cameras[0] = GameObject.FindGameObjectWithTag(ResourceConstant.CAMERA_TAG_ROOT).transform.Find("2DCamera").GetComponent<Camera>();
            cameras[1] = GameObject.FindGameObjectWithTag(ResourceConstant.CAMERA_TAG_ROOT).transform.Find("2.5DCamera").GetComponent<Camera>();
            rootCanvas = GameObject.FindGameObjectWithTag(ResourceConstant.UI_TAG_ROOT).GetComponent<Canvas>();
            Toggle toggle = Tool.GetComponentInChildren<Toggle>(panel, "Toggle");
            Tool.GetComponentInChildren<Button>(panel, "BackGame").onClick.AddListener(OnClick_BackGame);
            toggle.onValueChanged.AddListener(OnClick_TogglePerspective);
        }

        public override void OnEnter()
        {
            base.OnEnter();
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        /// <summary>
        /// 返回游戏
        /// </summary>
        public void OnClick_BackGame()
        {
            controller.close();
        }

        /// <summary>
        /// 切换视角(2.5D)
        /// </summary>
        public void OnClick_TogglePerspective(bool is_2_5D)
        {
            foreach (Camera camera in cameras)
            {
                if (camera.name.Contains("2D"))
                {
                    camera.gameObject.SetActive(!is_2_5D);
                    if (!is_2_5D)
                    {
                        rootCanvas.worldCamera = camera;
                        PlayerManager.Instance.Mine.togglePerspective(is_2_5D, camera);
                    }
                }
                else if (camera.name.Contains("2.5D"))
                {
                    camera.gameObject.SetActive(is_2_5D);
                    if (is_2_5D)
                    {
                        rootCanvas.worldCamera = camera;
                        PlayerManager.Instance.Mine.togglePerspective(is_2_5D, camera);
                    }
                }
            }
        }
    }
}