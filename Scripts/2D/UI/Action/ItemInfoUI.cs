namespace LAB2D.UI.Action
{
    using LAB2D;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Inventory;
    using LAB2D.UnityAdapter;
    using Character = LAB2D.Character.Character;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 点击对象展示UI — 通过 EventBus 订阅 InventoryGridChangedEvent 实现解耦更新。
    /// </summary>
    public class ItemInfoUI : MonoBehaviourInit
    {
        private string text;
        private Character character; // 实时跟踪角色
        private string select = string.Empty;
        private Vector3Int selectPos = default;

        /// <summary>
        /// 单例
        /// </summary>
        public static ItemInfoUI Instance { get; private set; }

        public void Awake()
        {
            Instance = this;
            EventBus.Instance.Subscribe<InventoryGridChangedEvent>(this.OnInventoryGridChanged);
        }

        public void OnDestroy()
        {
            EventBus.Instance.Unsubscribe<InventoryGridChangedEvent>(this.OnInventoryGridChanged);
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnInventoryGridChanged(InventoryGridChangedEvent e)
        {
            if (this.select.Equals(e.ManagerName)
                && this.selectPos.x == e.Position.X
                && this.selectPos.y == e.Position.Y)
            {
                // 通过 UnityAdapter 转换 Domain 坐标为 Unity 坐标，重新获取格式化文本
                Vector3Int unityPos = UnityVectorAdapter.ToVector3Int(e.Position);
                string cellInfo = InventoryManager.Instance.ToString(unityPos);
                ItemInfoPanel.Instance.SetItemInfo(cellInfo);
            }
        }

        public void Update()
        {
            // 实时更新Character信息
            if (this.character != null)
            {
                if (PanelController.Instance.Panels.Peek() != ItemInfoPanel.Instance)
                {
                    PanelController.Instance.Show(ItemInfoPanel.Instance);
                }

                ItemInfoPanel.Instance.SetItemInfo(this.character.ToString());
                ItemInfoPanel.Instance.SetCharacter(this.character);
            }

            if (UnityGlobalInputAdapter.GetSecondaryMouseDown())
            {
                List<RaycastResult> results = LAB2D.Tool.Tool.GetUIByMousePos();

                // 过滤不是滑动主屏幕的动作
                if (results.Count > 0 && results[0].gameObject.name != "Foreground")
                {
                    return;
                }

                this.selectPos = TileMap.Instance.GetMapPosByMouse();
                SelectUI selectUI;
                if (UnityGlobalInputAdapter.GetShowTileInfoHeld())
                {
                    selectUI = SelectManagerPool.Instance.CreateFreeSelect(this.selectPos);
                }
                else
                {
                    selectUI = SelectManagerPool.Instance.ReleaseAllAndGetOne();
                }

                do
                {
                    this.character = this.GetCharacter(this.selectPos);
                    if (this.character != null)
                    {
                        this.text = this.character.ToString();
                        SelectUI selectUI1 = SelectManagerPool.Instance.GetForCharacter(this.character);
                        if (selectUI1 != null)
                        {
                            selectUI = selectUI1;
                        }

                        selectUI.SetTarget(this.selectPos);
                        selectUI.Character = this.character;
                        ItemInfoPanel.Instance.SetCharacter(this.character);
                        break;
                    }

                    selectUI.SetTarget(this.selectPos);

                    // 掉落物或者仓库
                    this.select = "Resource";
                    this.text = this.GetResource(this.selectPos);
                    if (!this.text.Equals(string.Empty))
                    {
                        break;
                    }

                    // 没有物品,显示地图Tile
                    this.select = "Tile";
                    TileBase tileBase = this.GetTile(this.selectPos);
                    if (tileBase == null)
                    {
                        return;
                    }

                    this.text += $"{tileBase.name}\n" +
                        EnvironmentManager.Instance.ToString(this.selectPos);
                    break;
                }
                while (true);
                if (PanelController.Instance.Panels.Peek() != ItemInfoPanel.Instance)
                {
                    PanelController.Instance.Show(ItemInfoPanel.Instance);
                }

                ItemInfoPanel.Instance.SetItemInfo(this.text);
            }
        }

        /// <summary>
        /// 动态更新Tile信息
        /// </summary>
        /// <param name="name">类名，选中的类进行显示</param>
        /// <param name="pos">位置</param>
        /// <param name="text">文本信息</param>
        public void UpdateInfo(string name, Vector3Int pos, string text)
        {
            if (this.select.Equals(name) && this.selectPos.x == pos.x && this.selectPos.y == pos.y)
            {
                ItemInfoPanel.Instance.SetItemInfo(text);
            }
        }

        /// <summary>
        /// 获取角色
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <returns>角色信息</returns>
        public Character GetCharacter(Vector3Int posMap)
        {
            // 玩家
            Character character = PlayerManager.Instance.GetCharacterByPos(posMap);
            if (character == null)
            {
                // 敌人
                character = EnemyManager.Instance.GetCharacterByPos(posMap);
            }

            if (character == null)
            {
                // 工作者
                character = WorkerManager.Instance.GetCharacterByPos(posMap);
            }

            return character;
        }

        /// <summary>
        /// 获取TileBase
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <param name="isTile">是否是地图Tile</param>
        /// <param name="isResource">是否是资源Tile</param>
        /// <param name="isBuild">是否是建筑物</param>
        /// <returns>TileBase</returns>
        public TileBase GetTile(Vector3Int posMap, bool isTile = true, bool isResource = true, bool isBuild = true)
        {
            TileBase tileBase = null;
            if (isBuild)
            {
                tileBase = BuildMap.Instance.GetTile(posMap);
                if (tileBase != null)
                {
                    this.text = "Build:";
                    BuildItemData buildData = ItemDataManager.Instance.GetBuildItemDataByName(tileBase.name);
                    if (buildData != null)
                    {
                        this.text += $"\n可通行:{buildData.IsPass}\n需要建造:{buildData.IsNeedBuild}\n";
                    }
                }
            }

            // 如果点击的是床，则展示分配的Worker
            if (tileBase != null && tileBase.name.Contains("Bed"))
            {
                WorkerBedUI.Instance.ShowWorkerBed(posMap);
            }

            if (tileBase == null)
            {
                tileBase = ResourceMap.Instance.GetTile(posMap);
                if (tileBase != null)
                {
                    this.text = "Resource:";

                    // 手动添加任务
                    if (isResource && ResourceMap.Instance.TryGetGatherResourceInfo(posMap, out _))
                    {
                        GatherUI.Instance.SetPostion(posMap);
                    }
                    else if (isResource)
                    {
                        GatherUI.Instance.Hide();
                    }
                }
            }

            if (tileBase == null && isTile)
            {
                this.text = "Tile:";
                tileBase = TileMap.Instance.GetTile(posMap);
            }

            return tileBase;
        }

        /// <inheritdoc/>
        public override void Init()
        {
            base.Init();
            this.character = null;
            this.select = string.Empty;
            this.selectPos = default;
        }

        private string GetResource(Vector3Int posMap)
        {
            // 掉落物
            this.select = "DropResourceManager";
            string text = DropManager.Instance.ToString(posMap);
            if (text.Equals(string.Empty))
            {
                // 仓库
                this.select = "InventoryManager";
                text = InventoryManager.Instance.ToString(posMap);
                if (!text.Equals(string.Empty))
                {
                    InventoryManager.Instance.ShowWearMenu(posMap);
                }
            }

            return text;
        }
    }
}
