namespace LAB2D
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 仓库菜单面板
    /// </summary>
    public class InventoryMenuPanel : BasePanel<InventoryMenuPanel>
    {
        private readonly Transform position;
        private readonly Transform type;
        private readonly Transform id;
        private readonly Text content;

        public InventoryMenuPanel()
        {
            this.Name = "InventoryMenu";
            this.Init();
            this.position = Tool.GetComponentInChildren<Transform>(this.Panel, "Position");
            this.type = Tool.GetComponentInChildren<Transform>(this.Panel, "Type");
            this.id = Tool.GetComponentInChildren<Transform>(this.Panel, "Id");
            this.content = Tool.GetComponentInChildren<Text>(this.Panel, "Content");
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            Dictionary<Item.ItemType, Dictionary<Vector3Int, ResourceInfo>> typeToResource = InventoryManager.Instance.TypeToResource;
            int count = 0;
            foreach (KeyValuePair<Item.ItemType, Dictionary<Vector3Int, ResourceInfo>> pair in typeToResource)
            {
                if (count >= this.type.childCount)
                {
                    GameObject buttonItem = GameObject.Instantiate(ResourceManager.Instance.GetPrefab("ButtonItem"));
                    buttonItem.transform.SetParent(this.type);
                    buttonItem.transform.localScale = Vector3.one;
                }

                Button button = this.type.GetChild(count).GetComponent<Button>();
                string text1 = string.Empty;
                foreach (KeyValuePair<Vector3Int, ResourceInfo> valuePair in pair.Value)
                {
                    text1 += valuePair.Key.ToString() + ":" + valuePair.Value.ToString() + "\n";
                }

                button.onClick.AddListener(() =>
                {
                    this.content.text = text1;
                });
                Text text = button.transform.GetComponentInChildren<Text>();
                text.text = pair.Key.ToString();

                // TODO 删除Gameobject
                count++;
            }
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
        }
    }
}
