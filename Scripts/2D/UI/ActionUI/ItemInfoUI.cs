using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace LAB2D
{
    public class ItemInfoUI : MonoBehaviourInit
    {
        public static ItemInfoUI Instance { get; private set; }

        private string text;
        /// <summary>
        /// 实时跟踪角色
        /// </summary>
        private Character character;
        private string select = "";
        private Vector3Int selectPos = default;

        private void Awake()
        {
            Instance = this;
        }

        void Update()
        {
            // 实时更新Character信息
            if (character != null)
            {
                if(PanelController.Instance.Panels.Peek() != ItemInfoPanel.Instance)
                {
                    PanelController.Instance.show(ItemInfoPanel.Instance);
                }
                ItemInfoPanel.Instance.setItemInfo(character.ToString());
                ItemInfoPanel.Instance.setCharacter(character);
            }
            if (Input.GetMouseButtonDown(1))
            {
                List<RaycastResult> results = Tool.getUIByMousePos();
                // 过滤不是滑动主屏幕的动作
                if (results.Count > 0 && results[0].gameObject.name != "Foreground") return;
                selectPos = TileMap.Instance.worldPosToMapPos(Camera.main.ScreenToWorldPoint(Input.mousePosition));
                SelectUI selectUI;
                if (Input.GetKey(KeyCode.LeftControl))
                {
                    selectUI = SelectManager.Instance.getFreeSelect(selectPos);
                }
                else
                {
                    selectUI = SelectManager.Instance.getFirstAndFreeAll();
                }
                do {
                    character = getCharacter(selectPos);
                    if (character != null)
                    {
                        text = character.ToString();
                        SelectUI _selectUI = SelectManager.Instance.getByCharacter(character);
                        if(_selectUI != null)
                        {
                            selectUI = _selectUI;
                        }
                        selectUI.setTarget(selectPos);
                        selectUI.Character = character;
                        ItemInfoPanel.Instance.setCharacter(character);
                        break;
                    }
                    selectUI.setTarget(selectPos);
                    // 掉落物或者仓库
                    select = "Resource";
                    text = getResource(selectPos);
                    if (!text.Equals(""))
                    {
                        break;
                    }
                    // 没有物品,显示地图Tile
                    select = "Tile";
                    TileBase tileBase = getTile(selectPos);
                    if (tileBase == null) return;
                    text += $"{tileBase.name}\n" +
                        EnvironmentManager.Instance.ToString(selectPos);
                    break;
                } while (true);
                if (PanelController.Instance.Panels.Peek() != ItemInfoPanel.Instance)
                {
                    PanelController.Instance.show(ItemInfoPanel.Instance);
                }
                ItemInfoPanel.Instance.setItemInfo(text);
            }
        }

        /// <summary>
        /// 动态更新Tile信息
        /// </summary>
        /// <param name="name">类名，选中的类进行显示</param>
        /// <param name="text"></param>
        public void updateInfo(string name, Vector3Int pos, string text)
        {
            if (select.Equals(name) && selectPos.x==pos.x && selectPos.y == pos.y)
            {
                ItemInfoPanel.Instance.setItemInfo(text);
            }
        }

        public Character getCharacter(Vector3Int posMap)
        {
            // Player
            Character _character = PlayerManager.Instance.getCharacterByPos(posMap);
            if (_character == null)
            {
                // Enemy
                _character = EnemyManager.Instance.getCharacterByPos(posMap);
            }
            if (_character == null)
            {
                //  Worker
                _character = WorkerManager.Instance.getCharacterByPos(posMap);
            }
            return _character;
        }

        private string getResource(Vector3Int posMap) {
            // Drop
            select = "DropResourceManager";
            string text = DropResourceManager.Instance.ToString(posMap);
            if (text.Equals(""))
            {
                // Inventory
                select = "InventoryManager";
                text = InventoryManager.Instance.ToString(posMap);
                if (!text.Equals(""))
                {
                    InventoryManager.Instance.showWearMenu(posMap);
                }
            }
            return text;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="posMap"></param>
        /// <param name="isTile"></param>
        /// <param name="isUI"></param>
        /// <returns></returns>
        public TileBase getTile(Vector3Int posMap,bool isTile=true,bool isUI=true) {
            TileBase tileBase = BuildMap.Instance.getTile(posMap);
            text = "Build:";
            // 如果点击的是床，则展示分配的Worker
            if (tileBase != null && tileBase.name.Contains("Bed"))
            {
                WorkerBedUI.Instance.showWorkerBed(posMap);
            }
            if (tileBase == null)
            {
                text = "Resource:";
                tileBase = ResourceMap.Instance.getTile(posMap);
                // 手动添加任务
                if (tileBase != null && isUI) {
                    GatherUI.Instance.setPostion(posMap);
                }
            }
            if (tileBase == null && isTile)
            {
                text = "Tile:";
                tileBase = TileMap.Instance.getTile(posMap);
            }
            return tileBase;
        }

        public override void init()
        {
            base.init();
            character = null;
            select = "";
            selectPos = default;
        }
    }
}
