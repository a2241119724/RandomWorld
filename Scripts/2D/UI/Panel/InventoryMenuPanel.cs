using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class InventoryMenuPanel : BasePanel<InventoryMenuPanel>
    {
        private Transform position;
        private Transform type;
        private Transform id;
        private Text content;

        public InventoryMenuPanel()
        {
            Name = "InventoryMenu";
            setPanel();
            position = Tool.GetComponentInChildren<Transform>(panel, "Position");
            type = Tool.GetComponentInChildren<Transform>(panel, "Type");
            id = Tool.GetComponentInChildren<Transform>(panel, "Id");
            content = Tool.GetComponentInChildren<Text>(panel, "Content");
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Dictionary<ItemType, Dictionary<Vector3Int, ResourceInfo>> typeToResource = InventoryManager.Instance.TypeToResource;
            int count = 0;
            foreach(KeyValuePair<ItemType, Dictionary<Vector3Int, ResourceInfo>> pair in typeToResource)
            {
                if(count >= type.childCount)
                {
                    GameObject buttonItem = GameObject.Instantiate(ResourcesManager.Instance.getPrefab("ButtonItem"));
                    buttonItem.transform.SetParent(type);
                    buttonItem.transform.localScale = Vector3.one;
                }
                Button button = type.GetChild(count).GetComponent<Button>();
                string _text = "";
                foreach (KeyValuePair<Vector3Int, ResourceInfo> valuePair in pair.Value)
                {
                    _text += valuePair.Key.ToString() + ":" + valuePair.Value.ToString() + "\n";
                }
                button.onClick.AddListener(() =>
                {
                    content.text = _text;
                });
                Text text = button.transform.GetComponentInChildren<Text>();
                text.text = pair.Key.ToString();
                // TODO É¾³ýGameobject
                count++;
            }
        }

        public override void OnExit()
        {
            base.OnExit();
        }
    }
}
