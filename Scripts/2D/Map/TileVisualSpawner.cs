namespace LAB2D.Map
{
    using System.Collections.Generic;
    using LAB2D.Manager;
    using LAB2D.Render;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// Tilemap 视觉拆分器：把 tile 视觉渲染到独立 SpriteRenderer（挂 hostTransform 下
    /// "VisualSprites" 子节点，sortingLayer=Character），使建筑/树能参与按
    /// "视觉底端世界 y" 的全局排序（WorldYSortManager）。
    /// Tilemap 本体仍承担碰撞体/寻路/数据/存档/网络，其 TilemapRenderer 由宿主
    /// （BuildMap/ResourceMap）在 Awake 禁用，防双重渲染。
    ///
    /// 幂等约束：
    /// - CreateOrUpdate 以 tilemap 当前状态为准（GetSprite/GetColor/GetTransformMatrix）；
    ///   无 tile 的 cell 视为删除 → 多格物品副格（纯碰撞无 tile）自动不建视觉。
    /// - 每 cell 至多一个 SpriteRenderer，字典去重；删除用 Object.Destroy 延迟销毁
    ///   （WorldYSortManager 懒清扫兜底，无需显式 Unregister）。
    /// - RuleTile 的 sprite 依赖 8 邻域 tile，写路径在 SetTile/删除后调 RefreshAround
    ///   刷新墙角/直墙形态。
    /// </summary>
    public class TileVisualSpawner
    {
        private const string ParentName = "VisualSprites";

        private readonly Tilemap tilemap;
        private readonly Transform parent;
        private readonly string sortingLayerName;
        private readonly string objectNamePrefix;
        private readonly Material material; // tile 视觉材质（复制宿主 TilemapRenderer，保证拆分后仍接收 2D Light）
        private readonly Dictionary<Vector3Int, SpriteRenderer> visual = new Dictionary<Vector3Int, SpriteRenderer>();

        /// <summary>
        /// 注入 tilemap（数据源）与 hostTransform（视觉挂载点）。
        /// </summary>
        /// <param name="sortingLayerName">视觉 sprite 所在 sorting layer（须与角色同层才能交叉排序）。</param>
        /// <param name="objectNamePrefix">视觉对象命名前缀（如 "BuildVisual"）。</param>
        public TileVisualSpawner(Tilemap tilemap, Transform hostTransform, string sortingLayerName, string objectNamePrefix)
        {
            this.tilemap = tilemap;
            this.sortingLayerName = sortingLayerName;
            this.objectNamePrefix = objectNamePrefix;
            this.material = ResolveMaterial(tilemap);
            this.parent = new GameObject(ParentName).transform;
            this.parent.SetParent(hostTransform, false);

            // 诊断（事件点：每 TileVisualSpawner 构造一次）：暴露材质解析结果，验证拆分后仍接收 2D Light
            AWorkerTask.LogProvider(
                $"[BuildDiag] TileVisualSpawner layer={sortingLayerName} prefix={objectNamePrefix} " +
                $"mat={(this.material != null ? this.material.shader.name : "null(退默认unlit)")}",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>
        /// 解析 tile 视觉材质：优先复制宿主 TilemapRenderer 的材质（拆分前 Tilemap 用的
        /// lit 材质，保证建筑/树继续接收 2D Light）；宿主无 renderer 时退回 URP 2D lit shader。
        /// </summary>
        private static Material ResolveMaterial(Tilemap tilemap)
        {
            TilemapRenderer renderer = tilemap != null ? tilemap.GetComponent<TilemapRenderer>() : null;
            if (renderer != null && renderer.sharedMaterial != null)
            {
                return renderer.sharedMaterial;
            }

            Shader lit = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (lit != null)
            {
                return new Material(lit);
            }

            return null; // 极端兜底：不设置则退 SpriteRenderer 默认 unlit 材质（无光照但不会崩）
        }

        /// <summary>
        /// 以 tilemap 当前状态创建/更新 cell 的视觉（幂等，位置/图/颜色/缩放均同步）。
        /// cell 无 tile 时删除其视觉并返回 null。
        /// </summary>
        /// <param name="cell">地图坐标。</param>
        /// <returns>该 cell 的 SpriteRenderer（无 tile 时为 null）。</returns>
        public SpriteRenderer CreateOrUpdate(Vector3Int cell)
        {
            if (!this.tilemap.HasTile(cell))
            {
                this.Delete(cell);
                return null;
            }

            if (!this.visual.TryGetValue(cell, out SpriteRenderer sr))
            {
                GameObject go = new GameObject(this.NameOf(cell));
                go.transform.SetParent(this.parent, false);
                sr = go.AddComponent<SpriteRenderer>();
                sr.sortingLayerName = this.sortingLayerName;
                sr.sortingOrder = 0; // 每帧由 WorldYSortManager 统一分配
                if (this.material != null)
                {
                    sr.sharedMaterial = this.material; // 沿用宿主 lit 材质，保持接收 2D Light
                }

                this.visual.Add(cell, sr);
                WorldYSortManager.Ensure().Register(sr);
            }

            // 位置：GetCellCenterWorld 自动应用网格 transform（45° 转置坐标系）
            sr.transform.position = this.tilemap.GetCellCenterWorld(cell);

            // 图：GetSprite 可解析 RuleTile（依据 8 邻域）。引用变化才写，减少 SetProperty。
            Sprite sprite = this.tilemap.GetSprite(cell);
            if (sr.sprite != sprite)
            {
                sr.sprite = sprite;
            }

            // 颜色：建造中半透明/完成白色，与 SetColor 保持同步
            Color color = this.tilemap.GetColor(cell);
            if (sr.color != color)
            {
                sr.color = color;
            }

            // 缩放：RuleTile 翻转等 transform matrix 的影响（本项目的墙无翻转，恒为 1）。
            Vector3 scale = this.tilemap.GetTransformMatrix(cell).lossyScale;
            if (sr.transform.localScale != scale)
            {
                sr.transform.localScale = scale;
            }

            return sr;
        }

        /// <summary>
        /// 删除 cell 的视觉（tile 已移除/取消建造时）。
        /// </summary>
        /// <param name="cell">地图坐标。</param>
        public void Delete(Vector3Int cell)
        {
            if (!this.visual.TryGetValue(cell, out SpriteRenderer sr))
            {
                return;
            }

            this.visual.Remove(cell);
            if (sr != null)
            {
                Object.Destroy(sr.gameObject);
            }
        }

        /// <summary>
        /// 刷新 cell 周围 radius 邻域的视觉（RuleTile 邻居变化后更新墙角/直墙形态）。
        /// </summary>
        /// <param name="cell">中心坐标。</param>
        /// <param name="radius">邻域半径（默认 1 = 3x3）。</param>
        public void RefreshAround(Vector3Int cell, int radius = 1)
        {
            foreach (Vector3Int offset in Around(cell, radius))
            {
                this.CreateOrUpdate(offset);
            }
        }

        /// <summary>
        /// 全量重建：以 tilemap 当前状态为准（读档/全量网络同步后调用）。
        /// 先删除已无 tile 的视觉，再扫描 cellBounds 内全部有 tile 的 cell。
        /// </summary>
        public void RebuildAll()
        {
            List<Vector3Int> stale = new List<Vector3Int>();
            foreach (KeyValuePair<Vector3Int, SpriteRenderer> kv in this.visual)
            {
                if (!this.tilemap.HasTile(kv.Key))
                {
                    stale.Add(kv.Key);
                }
            }

            foreach (Vector3Int cell in stale)
            {
                this.Delete(cell);
            }

            BoundsInt bounds = this.tilemap.cellBounds;
            Vector3Int min = bounds.min;
            Vector3Int max = bounds.max;
            for (int x = min.x; x < max.x; x++)
            {
                for (int y = min.y; y < max.y; y++)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    if (this.tilemap.HasTile(cell))
                    {
                        this.CreateOrUpdate(cell);
                    }
                }
            }
        }

        /// <summary>
        /// 清空所有视觉（场景卸载/切换地图）。
        /// </summary>
        public void ClearAll()
        {
            foreach (KeyValuePair<Vector3Int, SpriteRenderer> kv in this.visual)
            {
                if (kv.Value != null)
                {
                    Object.Destroy(kv.Value.gameObject);
                }
            }

            this.visual.Clear();
        }

        /// <summary>
        /// 生成 cell 的 radius 邻域坐标集合（纯函数，供 RefreshAround 与单测复用）。
        /// </summary>
        /// <param name="cell">中心坐标。</param>
        /// <param name="radius">半径（>=0）。</param>
        /// <returns>包含中心在内的 (2*radius+1)^2 个坐标。</returns>
        public static Vector3Int[] Around(Vector3Int cell, int radius = 1)
        {
            int side = radius * 2 + 1;
            Vector3Int[] result = new Vector3Int[side * side];
            int i = 0;
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    result[i++] = new Vector3Int(cell.x + dx, cell.y + dy, cell.z);
                }
            }

            return result;
        }

        private string NameOf(Vector3Int cell)
        {
            return $"{this.objectNamePrefix}_{cell.x}_{cell.y}";
        }
    }
}
