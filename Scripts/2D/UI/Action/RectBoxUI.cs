namespace LAB2D.UI.Action
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.Domain.Common;
    using LAB2D.Serializable;
    using LAB2D.UnityAdapter;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.Tilemaps;
    using UnityEngine.UI;
    using GameCharacter = LAB2D.Character.Character;

    /// <summary>
    /// 拉矩形选框
    /// </summary>
    public class RectBoxUI : MonoBehaviour
    {
        private bool isDown = false;
        private Vector3 start;
        private Dictionary<TileTypeEnum, List<Vector3Int>> selects;
        private Transform options;

        /// <summary>
        /// Tile的类型
        /// </summary>
        public enum TileTypeEnum
        {
            /// <summary>
            /// 资源Tile
            /// </summary>
            Resource,
        }

        /// <summary>
        /// 单例
        /// </summary>
        public static RectBoxUI Instance { get; private set; }

        /// <summary>
        /// 确定对选中的所有Tile确定做出对应操作
        /// </summary>
        /// <param name="key">Tile类型</param>
        public void Onclick_Yes(TileTypeEnum key)
        {
            this.transform.position = ResourceConstant.VECTOR3_DEFAULT;
            this.options.gameObject.SetActive(false);
            this.selects[key].ForEach((posMap) =>
            {
                GameGridPosition gridPos = UnityVectorAdapter.ToGameGridPosition(posMap);
                if (ServiceLocator.Get<WorkerTaskManager>().GatherPositions.Contains(gridPos))
                {
                    return;
                }

                if (!ServiceLocator.Get<ResourceMap>().TryGetGatherResourceInfo(posMap, out ResourceInfo resourceInfo))
                {
                    return;
                }

                ServiceLocator.Get<WorkerTaskManager>().AddTask(
                    new WorkerGatherTask.GatherTaskBuilder()
                    .SetTarget(posMap).SetResourceInfo(resourceInfo).Build(), Vector3IntLAB.ToVector3IntLAB(posMap));
            });
        }

        /// <summary>
        /// 取消对选中的所有Tile确定做出对应操作
        /// </summary>
        /// <param name="key">Tile类型</param>
        public void Onclick_No(TileTypeEnum key)
        {
            this.transform.position = ResourceConstant.VECTOR3_DEFAULT;
            this.options.gameObject.SetActive(false);
            this.selects[key].ForEach((posMap) =>
            {
                GameGridPosition gridPos = UnityVectorAdapter.ToGameGridPosition(posMap);
                if (!ServiceLocator.Get<WorkerTaskManager>().GatherPositions.Contains(gridPos))
                {
                    return;
                }

                ServiceLocator.Get<WorkerTaskManager>().CancelGatherTask(gridPos);
                ServiceLocator.Get<GatherMap>().CancelGather(posMap);
            });
        }

        public void Awake()
        {
            Instance = this;
            ServiceLocator.Register(this);
            this.selects = new Dictionary<TileTypeEnum, List<Vector3Int>>
            {
                { TileTypeEnum.Resource, new List<Vector3Int>() },
            };
            this.options = LAB2D.Tool.Tool.GetComponentInChildren<Transform>(this.gameObject, "Options");
            this.options.gameObject.SetActive(false);
            Transform gather = LAB2D.Tool.Tool.GetComponentInChildren<Transform>(this.options.gameObject, "Gather");
            LAB2D.Tool.Tool.GetComponentInChildren<Button>(gather.gameObject, "Yes").onClick.AddListener(() =>
            {
                this.Onclick_Yes(TileTypeEnum.Resource);
            });
            LAB2D.Tool.Tool.GetComponentInChildren<Button>(gather.gameObject, "No").onClick.AddListener(() =>
            {
                this.Onclick_No(TileTypeEnum.Resource);
            });
        }

        public void Update()
        {
            if (this.options.gameObject.activeSelf)
            {
                if (UnityGlobalInputAdapter.GetSecondaryMouseDown() || UnityGlobalInputAdapter.GetMiddleMouseDown())
                {
                    this.options.gameObject.SetActive(false);
                }
                else if (UnityGlobalInputAdapter.GetPrimaryMouseDown())
                {
                    List<RaycastResult> results = LAB2D.Tool.Tool.GetUIByMousePos(TagConstant.ACTION_UI_TAG);

                    // 若没有点击到options UI, 则关闭options UI
                    if (results.Count == 0)
                    {
                        this.options.gameObject.SetActive(false);
                    }
                }

                return;
            }

            if (UnityGlobalInputAdapter.GetPrimaryMouseDown() && ServiceLocator.Get<PanelController>().Panels.Count > 0 &&
                (ServiceLocator.Get<PanelController>().Panels.Peek() == ServiceLocator.Get<ForegroundPanel>() ||
                ServiceLocator.Get<PanelController>().Panels.Peek() == ServiceLocator.Get<ItemInfoPanel>()))
            {
                this.options.gameObject.SetActive(false);
                Vector3 pos = UnityGlobalInputAdapter.GetMouseWorldPosition(Camera.main);
                this.start = pos;
                this.transform.position = new Vector3(pos.x, pos.y, 0.0f);
                this.isDown = true;
            }
            else if (this.isDown && ServiceLocator.Get<PanelController>().IsForeground())
            {
                Vector3 pos = UnityGlobalInputAdapter.GetMouseWorldPosition(Camera.main);
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

                ((RectTransform)this.transform).sizeDelta = new Vector2(System.Math.Abs(x), System.Math.Abs(y));
            }

            if (UnityGlobalInputAdapter.GetPrimaryMouseUp())
            {
                this.Select();
                ((RectTransform)this.transform).sizeDelta = Vector2.zero;
                this.isDown = false;
                this.options.gameObject.SetActive(this.selects[TileTypeEnum.Resource].Count > 0);
            }
        }

        /// <summary>
        /// 选择选中区域的所有物体
        /// </summary>
        private void Select()
        {
            foreach (TileTypeEnum key in this.selects.Keys)
            {
                this.selects[key].Clear();
            }

            ServiceLocator.Get<SelectManagerPool>().ReleaseAll();
            Vector3Int start = ServiceLocator.Get<TileMap>().WorldPosToMapPos(this.transform.position);
            Vector3Int end = ServiceLocator.Get<TileMap>().WorldPosToMapPos(new Vector3(
                this.transform.position.x + ((RectTransform)this.transform).sizeDelta.x,
                this.transform.position.y - ((RectTransform)this.transform).sizeDelta.y,
                this.transform.position.z));
            for (int i = start.x; i > end.x; i--)
            {
                for (int j = start.y; j < end.y; j++)
                {
                    Vector3Int posMap = new (i, j, 0);
                    GameCharacter character = ServiceLocator.Get<ItemInfoUI>().GetCharacter(posMap);

                    // 临近的位置可能会获得多个角色, 所以这里只取第一个
                    if (character != null && ServiceLocator.Get<SelectManagerPool>().GetForCharacter(character) == null)
                    {
                        SelectUI selectUI = ServiceLocator.Get<SelectManagerPool>().CreateFreeSelect(posMap);
                        selectUI.Character = character;
                    }

                    ResourceInfo resourceInfo = ServiceLocator.Get<DropManager>().GetDropByAll(posMap);
                    if (resourceInfo != null)
                    {
                        SelectUI selectUI = ServiceLocator.Get<SelectManagerPool>().CreateFreeSelect(posMap);
                        selectUI.SetTarget(posMap);
                    }

                    resourceInfo = ServiceLocator.Get<InventoryManager>().GetResourceByPos(posMap);
                    if (resourceInfo != null && resourceInfo.Id != -1 && resourceInfo.Count != 0)
                    {
                        SelectUI selectUI = ServiceLocator.Get<SelectManagerPool>().CreateFreeSelect(posMap);
                        selectUI.SetTarget(posMap);
                    }

                    TileBase tileBase = ServiceLocator.Get<ItemInfoUI>().GetTile(posMap, false, false, false);
                    if (tileBase != null)
                    {
                        SelectUI selectUI = ServiceLocator.Get<SelectManagerPool>().CreateFreeSelect(posMap);
                        selectUI.SetTarget(posMap);
                        if (ServiceLocator.Get<ResourceMap>().TryGetGatherResourceInfo(posMap, out _))
                        {
                            this.selects[TileTypeEnum.Resource].Add(posMap);
                        }
                    }
                }
            }
        }
    }
}
