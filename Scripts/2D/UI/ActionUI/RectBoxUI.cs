using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    /// <summary>
    /// 拉矩形选框
    /// </summary>
    public class RectBoxUI : MonoBehaviour
    {
        public static RectBoxUI Instance { private set; get; }

        private bool isDown = false;
        private Vector3 start;

        private void Awake()
        {
            Instance = this;
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                start = pos;
                transform.position = new Vector3(pos.x, pos.y, 0.0f);
                isDown = true;
            }
            else if (isDown && PanelController.Instance.Panels.Peek() == ForegroundPanel.Instance)
            {
                Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                float x = pos.x - start.x;
                float y = pos.y - start.y;
                if (x > 0 && y > 0)
                {
                    transform.position = new Vector3(start.x, start.y + y, 0.0f);
                }
                else if(x < 0 && y > 0)
                {
                    transform.position = new Vector3(start.x + x, start.y + y, 0.0f);
                }
                else if (x < 0 && y < 0)
                {
                    transform.position = new Vector3(start.x + x, start.y, 0.0f);
                }
                ((RectTransform)transform).sizeDelta = new Vector2(Mathf.Abs(x), Mathf.Abs(y));
            }
            if (Input.GetMouseButtonUp(0))
            {
                select();
                ((RectTransform)transform).sizeDelta = Vector2.zero;
                isDown = false;
            }
        }

        /// <summary>
        /// 选择选中区域的所有物体
        /// </summary>
        private void select()
        {
            SelectManager.Instance.freeAll();
            Vector3Int start = TileMap.Instance.worldPosToMapPos(transform.position);
            Vector3Int end = TileMap.Instance.worldPosToMapPos(new Vector3(
                transform.position.x + ((RectTransform)transform).sizeDelta.x,
                transform.position.y - ((RectTransform)transform).sizeDelta.y,
                transform.position.z));
            for(int i = start.x;i > end.x; i--)
            {
                for (int j = start.y; j < end.y; j++)
                {
                    Vector3Int posMap = new Vector3Int(i, j, 0);
                    Character character = ItemInfoUI.Instance.getCharacter(posMap);
                    if(character != null)
                    {
                        SelectUI selectUI = SelectManager.Instance.getFreeSelect(posMap);
                        selectUI.Character = character;
                    }
                    ResourceInfo resourceInfo = DropResourceManager.Instance.getDropByAll(posMap);
                    if(resourceInfo != null)
                    {
                        SelectUI selectUI = SelectManager.Instance.getFreeSelect(posMap);
                        selectUI.setTarget(posMap);
                    }
                    resourceInfo = InventoryManager.Instance.getByPos(posMap);
                    if (resourceInfo != null)
                    {
                        SelectUI selectUI = SelectManager.Instance.getFreeSelect(posMap);
                        selectUI.setTarget(posMap);
                    }
                    TileBase tileBase = ItemInfoUI.Instance.getTile(posMap,false);
                    if(tileBase != null)
                    {
                        SelectUI selectUI = SelectManager.Instance.getFreeSelect(posMap);
                        selectUI.setTarget(posMap);
                    }
                }
            }
        }
    }
}
