namespace LAB2D.UI.Panel
{
    using LAB2D;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 仓库菜单面板
    /// </summary>
    public class InventoryMenuPanel : ABasePanel<InventoryMenuPanel>
    {
        private readonly Transform position;
        private readonly Transform type;
        private readonly Transform id;
        private readonly Text content;

        public InventoryMenuPanel()
        {
            this.Name = "InventoryMenu";
            this.Init();
            this.position = LAB2D.Tool.Tool.GetComponentInChildren<Transform>(this.Panel, "Position");
            this.type = LAB2D.Tool.Tool.GetComponentInChildren<Transform>(this.Panel, "Type");
            this.id = LAB2D.Tool.Tool.GetComponentInChildren<Transform>(this.Panel, "Id");
            this.content = LAB2D.Tool.Tool.GetComponentInChildren<Text>(this.Panel, "Content");
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            Dictionary<AItem.ItemTypeEnum, Dictionary<Vector3Int, ResourceInfo>> typeToResource = InventoryManager.Instance.TypeToResource;
            int count = 0;
            foreach (KeyValuePair<AItem.ItemTypeEnum, Dictionary<Vector3Int, ResourceInfo>> pair in typeToResource)
            {
                if (count >= this.type.childCount)
                {
                    GameObject buttonItem = ResourceManager.Instance.Instantiate(PrefabConstant.BUTTON_ITEM);
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

        /// <inheritdoc/>
        public override void OnClick_Back()
        {
            this.Controller.Close();
        }
    }
}
