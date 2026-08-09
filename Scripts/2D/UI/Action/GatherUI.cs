namespace LAB2D.UI.Action
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.Domain.Common;
    using LAB2D.Map;
    using LAB2D.Serializable;
    using LAB2D.UnityAdapter;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 采集对应的UI
    /// </summary>
    public class GatherUI : MonoBehaviour
    {
        private Vector3Int posMap;

        /// <summary>
        /// 单例
        /// </summary>
        public static GatherUI Instance { get; private set; }

        public void Awake()
        {
            Instance = this;
            ServiceLocator.Register(this);
        }

        public void Start()
        {
            LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.gameObject, "Yes").onClick.AddListener(this.Onclick_Yes);
            LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.gameObject, "No").onClick.AddListener(this.Onclick_No);
        }

        public void Update()
        {
            // 左键/右键点击空白处时将采集UI移到屏幕外（隐藏）
            // 点击在 UI 元素上时不隐藏，避免误吞按钮点击
            if ((UnityGlobalInputAdapter.GetPrimaryMouseDown() || UnityGlobalInputAdapter.GetSecondaryMouseDown())
                && this.transform.position.x != ResourceConstant.VECTOR3_DEFAULT.x
                && this.IsClickOnEmptySpace())
            {
                this.transform.position = ResourceConstant.VECTOR3_DEFAULT;
            }
        }

        /// <summary>
        /// 检测当前鼠标点击是否在空白处（非 UI 元素上）
        /// </summary>
        private bool IsClickOnEmptySpace()
        {
            var uiResults = LAB2D.Tool.Tool.GetUIByMousePos(TagConstant.UI_TAG);
            if (uiResults.Count > 0 && uiResults[0].gameObject.name != "Foreground")
            {
                return false;
            }

            var actionResults = LAB2D.Tool.Tool.GetUIByMousePos(TagConstant.ACTION_UI_TAG);
            if (actionResults.Count > 0 && actionResults[0].gameObject.name != "Foreground")
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 设置采集UI的位置
        /// </summary>
        /// <param name="posMap">位置</param>
        public void SetPostion(Vector3Int posMap)
        {
            this.posMap = posMap;
            this.transform.position = ServiceLocator.Get<TileMap>().MapPosToWorldPos(posMap);
        }

        /// <summary>
        /// 隐藏采集UI。
        /// </summary>
        public void Hide()
        {
            this.transform.position = ResourceConstant.VECTOR3_DEFAULT;
        }

        /// <summary>
        /// 确定采集 — 发布为悬赏任务，所得物品归 Player，Worker 获得赏金。
        /// 支持 ResourceMap 资源采集和 TileMap 地形挖掘两种类型。
        /// </summary>
        public void Onclick_Yes()
        {
            this.Hide();
            GameGridPosition gridPos = UnityVectorAdapter.ToGameGridPosition(this.posMap);
            if (ServiceLocator.Get<WorkerTaskManager>().GatherPositions.Contains(gridPos))
            {
                return;
            }

            var player = ServiceLocator.Get<PlayerManager>().Mine;
            var bountyService = ServiceLocator.Get<PlayerBountyService>();

            // 优先尝试 ResourceMap 资源采集
            if (ServiceLocator.Get<ResourceMap>().TryGetGatherResourceInfo(this.posMap, out ResourceInfo resourceInfo))
            {
                bountyService.PostGatherBounty(player, this.posMap, resourceInfo.Id);
                return;
            }

            // 回退：检查是否为可挖掘地形（如山）
            TileMap tileMap = ServiceLocator.Get<TileMap>();
            TerrainConfigDatabase terrainDb = ServiceLocator.Get<TerrainConfigDatabase>();
            if (tileMap?.TileMapDataLAB?.MapTiles != null && terrainDb != null)
            {
                int terrainId = tileMap.TileMapDataLAB.MapTiles[this.posMap.x, this.posMap.y];
                if (terrainDb.IsDiggable(terrainId))
                {
                    bountyService.PostDigBounty(player, this.posMap, terrainId);
                }
            }
        }

        /// <summary>
        /// 取消采集
        /// </summary>
        public void Onclick_No()
        {
            this.Hide();
            GameGridPosition gridPos = UnityVectorAdapter.ToGameGridPosition(this.posMap);
            if (!ServiceLocator.Get<WorkerTaskManager>().GatherPositions.Contains(gridPos))
            {
                return;
            }

            ServiceLocator.Get<WorkerTaskManager>().CancelGatherTask(gridPos);
            ServiceLocator.Get<GatherMap>().CancelGather(this.posMap);
        }
    }
}
