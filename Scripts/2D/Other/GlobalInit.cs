using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class GlobalInit : MonoBehaviour
    {
        public static GlobalInit Instance { get; private set; }
        public bool initPanel = false;
        public bool initFont = true;

        private GameObject tip; // 提示框预制体
        private readonly List<string> fontExcludeText = new List<string>() {
            "Label"
        };
        private BuildingUI buildingUI;

        void Awake()
        {
            Instance = this;
            tip = ResourcesManager.Instance.getPrefab("Tip");
            if (initFont)
            {
                // init font
                Text[] texts = FindObjectsOfType<Text>();
                foreach (Text text in texts)
                {
                    if (fontExcludeText.Contains(text.name)) continue;
                    text.fontSize = 40;
                }
            }
        }

        void Start()
        {
            // init panel
            if (initPanel)
            {
                ForegroundPanel.Instance.init();
                if (PanelController.Instance == null)
                {
                    Debug.LogError("manager Not Found!!!");
                    return;
                }
                PanelController.Instance.show(CreateOrJoinPanel.Instance);
            }
        }

        public void showTip(string text)
        {
            GameObject g = Instantiate(tip);
            if (g == null) {
                Debug.LogError("tip Instantiate Error!!!");
                return;
            }
            g.name = tip.name;
            g.GetComponent<TipUI>().setText(text);
            g.transform.SetParent(transform,false);
            // 由于实例化产生形状变化,重新设置
            //g.transform.localScale = Vector3.zero;
            //RectTransform rt = g.GetComponent<RectTransform>();
            //rt.offsetMin = Vector2.zero;
            //rt.offsetMax = Vector2.zero;
            //Vector3 v = rt.localPosition; // 相对坐标
            //rt.localPosition = new Vector3(v.x, v.y, 0);
        }

        private void Update()
        {
            // 退出界面(除了ForegroundPanel)
            if (Input.GetKey(KeyCode.Escape))
            {
                if (PanelController.Instance.Panels.Count == 0)
                {
                    if (buildingUI == null)
                    {
                        buildingUI = GameObject.FindGameObjectWithTag(ResourceConstant.UI_TAG_ROOT).GetComponent<BuildingUI>();
                    }
                    buildingUI.enabled = false;
                    PanelController.Instance.show(BuildMenuPanel.Instance);
                    IsAvailableMap.Instance.clearShow();
                }
                else if(PanelController.Instance.Panels.Peek() != ForegroundPanel.Instance)
                {
                    PanelController.Instance.close();
                }
            }
        }
    }

    public abstract class MonoBehaviourInit : MonoBehaviour {
        public virtual void init() { }
    }
}