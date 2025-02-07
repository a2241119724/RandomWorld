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

        private GameObject tip; // ��ʾ��Ԥ����
        private readonly List<string> fontExcludeText = new List<string>() {
            "Label"
        };

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
                // ��ʼ������
                BackpackMenuPanel.Instance.panel.SetActive(true);
                BackpackMenuPanel.Instance.panel.SetActive(false);
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
            // ����ʵ����������״�仯,��������
            //g.transform.localScale = Vector3.zero;
            //RectTransform rt = g.GetComponent<RectTransform>();
            //rt.offsetMin = Vector2.zero;
            //rt.offsetMax = Vector2.zero;
            //Vector3 v = rt.localPosition; // �������
            //rt.localPosition = new Vector3(v.x, v.y, 0);
        }

        private void Update()
        {
            // �˳�����(����ForegroundPanel,CreateOrJoinPanel,CreateMenuPanel,CreateDataPanel,AsyncProgressPanel)
            if (Input.GetKey(KeyCode.Escape))
            {
                if (PanelController.Instance.Panels.Count == 0)
                {
                    BuildingUI.Instance.enabled = false;
                    PanelController.Instance.show(BuildMenuPanel.Instance);
                    IsAvailableMap.Instance.clearShow();
                }
                else if(PanelController.Instance.Panels.Peek() != ForegroundPanel.Instance &&
                    PanelController.Instance.Panels.Peek() != CreateOrJoinPanel.Instance &&
                    PanelController.Instance.Panels.Peek() != CreateMenuPanel.Instance &&
                    PanelController.Instance.Panels.Peek() != CreateDataPanel.Instance &&
                    PanelController.Instance.Panels.Peek() != AsyncProgressPanel.Instance)
                {
                    PanelController.Instance.close();
                }
            }
            EnvironmentManager.Instance.updateEnergy();
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(2))
            {
                // �ر�ItemInfo���
                if (PanelController.Instance.Panels.Count > 0 
                    && PanelController.Instance.Panels.Peek() == ItemInfoPanel.Instance)
                {
                    PanelController.Instance.close();
                }
            }
        }
    }

    public abstract class MonoBehaviourInit : MonoBehaviour {
        public virtual void init() { }
    }

    public interface ISaveData
    {
        void loadData();
        void saveData();
    }

    public abstract class ASaveData : ISaveData
    {
        public static List<ISaveData> Instances = new List<ISaveData>();

        public ASaveData()
        {
            Instances.Add(this);
        }

        public virtual void loadData() { }
        public virtual void saveData() { }
    }

    public abstract class AMonoSaveData : MonoBehaviour,ISaveData
    {
        public static List<ISaveData> Instances = new List<ISaveData>();

        public AMonoSaveData()
        {
            Instances.Add(this);
        }

        public virtual void loadData() { }
        public virtual void saveData() { }
    }
}