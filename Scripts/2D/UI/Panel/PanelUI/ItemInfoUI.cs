using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace LAB2D
{
    public class ItemInfoUI : MonoBehaviour
    {
        public static ItemInfoUI Instance { get; private set; }

        private string text = "";
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
            }
            if (Input.GetMouseButtonDown(0))
            {
                List<RaycastResult> results = Tool.getUIByMousePos();
                // 过滤不是滑动主屏幕的动作
                if (results.Count > 0 && results[0].gameObject.name != "Foreground") return;
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector3Int posMap = TileMap.Instance.worldPosToMapPos(worldPos);
                selectPos = posMap;
                SelectUI.Instance.Character = null;
                character = null;
                SelectUI.Instance.Target = worldPos;
                do {
                    Character _character = getCharacter(posMap);
                    if (_character != null)
                    {
                        text = _character.ToString();
                        SelectUI.Instance.Character = _character;
                        character = _character;
                        SelectUI.Instance.Target = _character.transform.position;
                        break;
                    }
                    select = "Resource";
                    text = getResource(posMap);
                    if (!text.Equals(""))
                    {
                        break;
                    }
                    // 没有物品,显示地图Tile
                    select = "Tile";
                    TileBase tileBase = getTile(posMap);
                    if (tileBase == null) return;
                    text += $"{tileBase.name}\n" +
                        EnvironmentManager.Instance.ToString(posMap);
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

        private Character getCharacter(Vector3Int posMap)
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
            }
            return text;
        }

        private TileBase getTile(Vector3Int posMap) {
            TileBase tileBase = BuildMap.Instance.BuildTileMap.GetTile(posMap);
            text = "Build:";
            if (tileBase == null)
            {
                text = "Resource:";
                tileBase = ResourceMap.Instance.getTile(posMap);
            }
            if (tileBase == null)
            {
                text = "Tile:";
                tileBase = TileMap.Instance.getTile(posMap);
            }
            return tileBase;
        }
    }
}
