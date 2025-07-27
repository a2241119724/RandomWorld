namespace LAB2D
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;
    using UnityEngine.UI;

    /// <summary>
    /// 拉矩形选框
    /// </summary>
    public class RectBoxUI : MonoBehaviour
    {
        private bool isDown = false;
        private Vector3 start;
        private Dictionary<TileType, List<Vector3Int>> selects;
        private Transform options;

        /// <summary>
        /// 单例
        /// </summary>
        public static RectBoxUI Instance { get; private set; }

        /// <summary>
        /// 确定对选中的所有Tile确定做出对应操作
        /// </summary>
        /// <param name="key">Tile类型</param>
        public void Onclick_Yes(TileType key)
        {
            this.transform.position = ResourceConstant.VECTOR3_DEFAULT;
            this.options.gameObject.SetActive(false);
            this.selects[key].ForEach((posMap) =>
            {
                TileBase tileBase = ResourceMap.Instance.GetTile(posMap);
                if (tileBase == null)
                {
                    return;
                }

                if (WorkerTaskManager.Instance.GatherPos.Contains(posMap))
                {
                    return;
                }

                WorkerTaskManager.Instance.AddTask(new WorkerGatherTask.GatherTaskBuilder()
                    .SetTarget(posMap).SetGatherName(tileBase.name).Build());
            });
        }

        /// <summary>
        /// 取消对选中的所有Tile确定做出对应操作
        /// </summary>
        /// <param name="key">Tile类型</param>
        public void Onclick_No(TileType key)
        {
            this.transform.position = ResourceConstant.VECTOR3_DEFAULT;
            this.options.gameObject.SetActive(false);
            this.selects[key].ForEach((posMap) =>
            {
                if (!WorkerTaskManager.Instance.GatherPos.Contains(posMap))
                {
                    return;
                }

                WorkerTaskManager.Instance.CancelGatherTask(posMap);
                GatherMap.Instance.CancelGather(posMap);
            });
        }

        private void Awake()
        {
            Instance = this;
            this.selects = new Dictionary<TileType, List<Vector3Int>>
            {
                { TileType.Resource, new List<Vector3Int>() },
            };
            this.options = Tool.GetComponentInChildren<Transform>(this.gameObject, "Options");
            this.options.gameObject.SetActive(false);
            Transform gather = Tool.GetComponentInChildren<Transform>(this.options.gameObject, "Gather");
            Tool.GetComponentInChildren<Button>(gather.gameObject, "Yes").onClick.AddListener(() =>
            {
                this.Onclick_Yes(TileType.Resource);
            });
            Tool.GetComponentInChildren<Button>(gather.gameObject, "No").onClick.AddListener(() =>
            {
                this.Onclick_No(TileType.Resource);
            });
        }

        private void Update()
        {
            if (this.options.gameObject.activeSelf)
            {
                if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
                {
                    this.options.gameObject.SetActive(false);
                }

                return;
            }

            if (Input.GetMouseButtonDown(0) && PanelController.Instance.Panels.Count > 0 &&
                (PanelController.Instance.Panels.Peek() == ForegroundPanel.Instance ||
                PanelController.Instance.Panels.Peek() == ItemInfoPanel.Instance))
            {
                this.options.gameObject.SetActive(false);
                Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                this.start = pos;
                this.transform.position = new Vector3(pos.x, pos.y, 0.0f);
                this.isDown = true;
            }
            else if (this.isDown && PanelController.Instance.IsForeground())
            {
                Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                float x = pos.x - this.start.x;
                float y = pos.y - this.start.y;
                if (x > 0 && y > 0)
                {
                    this.transform.position = new Vector3(this.start.x, this.start.y + y, 0.0f);
                }
                else if (x < 0 && y > 0)
                {
                    this.transform.position = new Vector3(this.start.x + x, this.start.y + y, 0.0f);
                }
                else if (x < 0 && y < 0)
                {
                    this.transform.position = new Vector3(this.start.x + x, this.start.y, 0.0f);
                }

                ((RectTransform)this.transform).sizeDelta = new Vector2(Mathf.Abs(x), Mathf.Abs(y));
            }

            if (Input.GetMouseButtonUp(0))
            {
                this.Select();
                ((RectTransform)this.transform).sizeDelta = Vector2.zero;
                this.isDown = false;
                this.options.gameObject.SetActive(this.selects[TileType.Resource].Count > 0);
            }
        }

        /// <summary>
        /// 选择选中区域的所有物体
        /// </summary>
        private void Select()
        {
            foreach (TileType key in this.selects.Keys)
            {
                this.selects[key].Clear();
            }

            SelectManagerPool.Instance.FreeAll();
            Vector3Int start = TileMap.Instance.WorldPosToMapPos(this.transform.position);
            Vector3Int end = TileMap.Instance.WorldPosToMapPos(new Vector3(
                this.transform.position.x + ((RectTransform)this.transform).sizeDelta.x,
                this.transform.position.y - ((RectTransform)this.transform).sizeDelta.y,
                this.transform.position.z));
            for (int i = start.x; i > end.x; i--)
            {
                for (int j = start.y; j < end.y; j++)
                {
                    Vector3Int posMap = new (i, j, 0);
                    Character character = ItemInfoUI.Instance.GetCharacter(posMap);
                    if (character != null)
                    {
                        SelectUI selectUI = SelectManagerPool.Instance.CreateFreeSelect(posMap);
                        selectUI.Character = character;
                    }

                    ResourceInfo resourceInfo = DropResourceManager.Instance.GetDropByAll(posMap);
                    if (resourceInfo != null)
                    {
                        SelectUI selectUI = SelectManagerPool.Instance.CreateFreeSelect(posMap);
                        selectUI.SetTarget(posMap);
                    }

                    resourceInfo = InventoryManager.Instance.GetResourceByPos(posMap);
                    if (resourceInfo != null)
                    {
                        SelectUI selectUI = SelectManagerPool.Instance.CreateFreeSelect(posMap);
                        selectUI.SetTarget(posMap);
                    }

                    TileBase tileBase = ItemInfoUI.Instance.GetTile(posMap, false, false);
                    if (tileBase != null)
                    {
                        SelectUI selectUI = SelectManagerPool.Instance.CreateFreeSelect(posMap);
                        selectUI.SetTarget(posMap);
                        this.selects[TileType.Resource].Add(posMap);
                    }
                }
            }
        }

        /// <summary>
        /// Tile的类型
        /// </summary>
        public enum TileType
        {
            /// <summary>
            /// 资源Tile
            /// </summary>
            Resource,
        }
    }
}
