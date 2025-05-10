using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

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
        private Dictionary<string, List<Vector3Int>> selects;
        private Transform options;

        private void Awake()
        {
            Instance = this;
            selects = new Dictionary<string, List<Vector3Int>>();
            selects.Add("Resource", new List<Vector3Int>());
            options = Tool.GetComponentInChildren<Transform>(gameObject, "Options");
            options.gameObject.SetActive(false);
            Transform gather = Tool.GetComponentInChildren<Transform>(options.gameObject, "Gather");
            Tool.GetComponentInChildren<Button>(gather.gameObject, "Yes").onClick.AddListener(() =>
            {
                Onclick_Yes("Resource");
            });
            Tool.GetComponentInChildren<Button>(gather.gameObject, "No").onClick.AddListener(() =>
            {
                Onclick_No("Resource");
            });
        }

        void Update()
        {
            if (options.gameObject.activeSelf)
            {
                if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
                {
                    options.gameObject.SetActive(false);
                }
                return;
            }
            if (Input.GetMouseButtonDown(0) && PanelController.Instance.Panels.Count > 0 && 
                (PanelController.Instance.Panels.Peek() == ForegroundPanel.Instance ||
                PanelController.Instance.Panels.Peek() == ItemInfoPanel.Instance))
            {
                options.gameObject.SetActive(false);
                Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                start = pos;
                transform.position = new Vector3(pos.x, pos.y, 0.0f);
                isDown = true;
            }
            else if (isDown && PanelController.Instance.isForeground())
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
                options.gameObject.SetActive(selects["Resource"].Count > 0);
            }
        }

        /// <summary>
        /// 选择选中区域的所有物体
        /// </summary>
        private void select()
        {
            foreach(string key in selects.Keys)
            {
                selects[key].Clear();
            }
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
                    TileBase tileBase = ItemInfoUI.Instance.getTile(posMap,false,false);
                    if(tileBase != null)
                    {
                        SelectUI selectUI = SelectManager.Instance.getFreeSelect(posMap);
                        selectUI.setTarget(posMap);
                        selects["Resource"].Add(posMap);
                    }
                }
            }
        }

        public void Onclick_Yes(string key)
        {
            transform.position = ResourceConstant.VECTOR3_DEFAULT;
            options.gameObject.SetActive(false);
            selects[key].ForEach((posMap) =>
            {
                TileBase tileBase = ResourceMap.Instance.getTile(posMap);
                if (tileBase == null) return;
                if (WorkerTaskManager.Instance.GatherPos.Contains(posMap)) return;
                WorkerTaskManager.Instance.addTask(new WorkerGatherTask.GatherTaskBuilder()
                    .setTarget(posMap).setGatherName(tileBase.name).build());
            });
        }

        public void Onclick_No(string key)
        {
            transform.position = ResourceConstant.VECTOR3_DEFAULT;
            options.gameObject.SetActive(false);
            selects[key].ForEach((posMap) =>
            {
                if (!WorkerTaskManager.Instance.GatherPos.Contains(posMap)) return;
                WorkerTaskManager.Instance.cancelGatherTask(posMap);
                GatherMap.Instance.cancelGather(posMap);
            });
        }
    }
}
