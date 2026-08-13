namespace LAB2D.Map
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Core;
    using LAB2D.Domain.Common;
    using LAB2D.Serializable;
    using LAB2D.UnityAdapter;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 地图 — 同时实现 ITileMapQuery 以支持其他层通过接口查询地图。
    ///
    /// 地形类型完全由 TerrainConfigDatabase 数据驱动
    /// （Resources/SO/TerrainConfigs/ 下的 TerrainTileConfig .asset 文件）。
    /// </summary>
    public class TileMap : BaseTileMap, ITileMapQuery
    {
        /// <summary>
        /// 默认地图高度（存档不可用时的回退尺寸）
        /// </summary>
        public const int DefaultHeight = 548;

        /// <summary>
        /// 默认地图宽度（存档不可用时的回退尺寸）
        /// </summary>
        public const int DefaultWidth = 548;

        /// <summary>
        /// 单例
        /// </summary>
        public static TileMap Instance { get; private set; }

        /// <summary>
        /// 地图数据
        /// </summary>
        public TileMapData TileMapDataLAB { get; private set; }

        /// <summary>
        /// 地形生成策略（默认使用 RandomScatterFillGenerator）
        /// </summary>
        private ITerrainGenerator generator;

        /// <summary>
        /// 缓存的海洋水格 ID，用于边界检查（避免每次调用 ServiceLocator）。
        /// </summary>
        private int cachedWaterTerrainId = -1;

        /// <summary>
        /// 每个 Chunk 的边长（瓦片数）。由 ComputeChunkSize 根据地图尺寸动态计算，
        /// 保证总 Chunk 数 ≤ MaxChunksPerDim²。
        /// </summary>
        private static int chunkSize = 64;

        /// <summary>
        /// 目标每维度最大 Chunk 数。总 Chunk = MaxChunksPerDim²。
        /// </summary>
        private const int MaxChunksPerDim = 2;

        /// <summary>
        /// 根据地图尺寸计算 Chunk 边长，保证总 Chunk 数不超过 MaxChunksPerDim²。
        /// </summary>
        private void ComputeChunkSize(int mapWidth, int mapHeight)
        {
            int maxDim = Mathf.Max(mapWidth, mapHeight);
            chunkSize = Mathf.Max(Mathf.CeilToInt((float)maxDim / MaxChunksPerDim), 32);
        }

        /// <summary>
        /// 幽灵瓦片的透明颜色缓存 — 幽灵瓦片仅供 Rule Tile 查询邻居，不参与渲染。
        /// </summary>
        private static readonly Color GhostTransparentColor = new Color(1f, 1f, 1f, 0f);

        /// <summary>
        /// Chunk 索引 → Tilemap 组件的映射。
        /// </summary>
        private readonly Dictionary<Vector2Int, Tilemap> chunkTilemaps = new Dictionary<Vector2Int, Tilemap>();

        /// <summary>
        /// 所有 Chunk 的父节点 Transform（Grid 的子节点，与 TileMap 同级）。
        /// </summary>
        private Transform chunksRoot;

        /// <summary>
        /// 缓存的 Chunk 索引 — 用于 ShowTilemap 顺序遍历时避免反复字典查找。
        /// </summary>
        private Vector2Int cachedChunkIndex = new Vector2Int(int.MinValue, int.MinValue);

        /// <summary>
        /// 缓存的 Chunk Tilemap — 对应 cachedChunkIndex。
        /// </summary>
        private Tilemap cachedChunkTilemap;

        /// <summary>
        /// 缓存的 Grid cellSize — 用于 Chunk 定位（考虑 YXZ swizzle）。
        /// </summary>
        private Vector3 gridCellSize = Vector3.one;

        /// <summary>
        /// 缓存的原始 TilemapRenderer 材质，复制给 Chunk TilemapRenderer。
        /// </summary>
        private Material chunkMaterial;

        /// <summary>
        /// 缓存的原始 TilemapRenderer sortingLayerID。
        /// </summary>
        private int chunkSortingLayerID;

        /// <summary>
        /// 缓存的原始 TilemapRenderer sortingOrder。
        /// </summary>
        private int chunkSortingOrder;

        /// <summary>
        /// 缓存的原始 GameObject layer，复制给 Chunk GameObject。
        /// </summary>
        private int chunkLayer;

        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();
            Instance = this;

            if (!ServiceLocator.TryGet(out this.generator))
            {
                this.generator = new RandomScatterFillGenerator();
            }

            // 缓存水格 ID，避免边界检查时频繁 ServiceLocator 查找
            if (ServiceLocator.TryGet(out TerrainConfigDatabase db))
            {
                this.cachedWaterTerrainId = db.GetWaterTerrainId();
            }

            // 初始化 Chunk 系统
            this.InitChunkSystem();

            // chunksRoot 使用 DontSaveInEditor：Editor 退出 Play Mode 时 Unity 自动销毁，
            // 在 Undo 序列化之前完成，无需手动清理。Build 中无 Undo 系统，无需处理。
        }

        /// <summary>
        /// 初始化 Chunk 系统：获取 Grid 配置、创建 Chunk 父节点、
        /// 缓存渲染器配置、禁用原始 TilemapRenderer。
        /// </summary>
        private void InitChunkSystem()
        {
            Grid grid = this.GetComponentInParent<Grid>();
            if (grid != null)
            {
                this.gridCellSize = grid.cellSize;
            }

            // 缓存原始 GameObject 的 layer，供 Chunk 使用
            this.chunkLayer = this.gameObject.layer;

            // chunksRoot 使用 DontSaveInEditor：Unity Editor 退出时自动销毁，在 Undo 序列化之前
            this.chunksRoot = new GameObject("TileChunks").transform;
            this.chunksRoot.gameObject.hideFlags = HideFlags.DontSaveInEditor;
            this.chunksRoot.SetParent(this.transform);
            this.chunksRoot.localPosition = Vector3.zero;

            // CompositeCollider2D 合并所有 Chunk 碰撞体：各 Chunk 的 TilemapCollider2D
            // 设 usedByComposite=true 后，其独立几何体被复合体"吸收"，退出时只需序列化
            // 1 个复合体 + N 个轻量 Collider，而非 N 个完整碰撞几何体，解决退出卡死。
            Rigidbody2D rb = this.chunksRoot.gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            rb.hideFlags = HideFlags.DontSaveInEditor;
            CompositeCollider2D cc = this.chunksRoot.gameObject.AddComponent<CompositeCollider2D>();
            cc.hideFlags = HideFlags.DontSaveInEditor;
            cc.geometryType = CompositeCollider2D.GeometryType.Polygons;

            // 缓存原始 TilemapRenderer 配置，供 Chunk TilemapRenderer 复制
            TilemapRenderer originalRenderer = this.GetComponent<TilemapRenderer>();
            if (originalRenderer != null)
            {
                this.chunkMaterial = originalRenderer.material;
                this.chunkSortingLayerID = originalRenderer.sortingLayerID;
                this.chunkSortingOrder = originalRenderer.sortingOrder;
                // 禁用原始 Renderer：主 Tilemap 不再直接存储瓦片
                originalRenderer.enabled = false;
            }

            // 禁用原始 TilemapCollider2D：主 Tilemap 不再直接存储瓦片，
            // 碰撞体由各 Chunk 的 TilemapCollider2D 生成
            TilemapCollider2D originalCollider = this.GetComponent<TilemapCollider2D>();
            if (originalCollider != null)
            {
                originalCollider.enabled = false;
            }
        }

        /// <summary>
        /// 计算地图坐标 (x, y) 所属的 Chunk 索引。
        /// </summary>
        private static Vector2Int GetChunkIndex(int x, int y)
        {
            return new Vector2Int(
                Mathf.FloorToInt((float)x / chunkSize),
                Mathf.FloorToInt((float)y / chunkSize));
        }

        /// <summary>
        /// 计算地图坐标 (x, y) 在 Chunk (cx, cy) 内的局部坐标。
        /// </summary>
        private static Vector3Int GetLocalPos(int x, int y, int cx, int cy)
        {
            return new Vector3Int(x - (cx * chunkSize), y - (cy * chunkSize), 0);
        }

        /// <summary>
        /// 获取或创建指定索引的 Chunk Tilemap。
        /// Chunk 定位需考虑 Grid 的 YXZ swizzle：
        /// chunk (cx, cy) → Grid-local position = (cy*S*cellSize.y, cx*S*cellSize.x, 0)
        /// </summary>
        private Tilemap GetOrCreateChunk(Vector2Int chunkIndex)
        {
            if (this.chunkTilemaps.TryGetValue(chunkIndex, out Tilemap existing))
            {
                return existing;
            }

            int cx = chunkIndex.x;
            int cy = chunkIndex.y;

            GameObject chunkGO = new GameObject($"Chunk_{cx}_{cy}");
            chunkGO.hideFlags = HideFlags.DontSaveInEditor; // Editor 退出时自动销毁，在 Undo 序列化之前
            chunkGO.layer = this.chunkLayer;
            chunkGO.transform.SetParent(this.chunksRoot);
            // YXZ swizzle: chunk (cx, cy) 定位在 (cy*S*cellSize.y, cx*S*cellSize.x)
            chunkGO.transform.localPosition = new Vector3(
                cy * chunkSize * this.gridCellSize.y,
                cx * chunkSize * this.gridCellSize.x,
                0f);

            Tilemap tm = chunkGO.AddComponent<Tilemap>();
            tm.hideFlags = HideFlags.DontSaveInEditor; // GameObject 的 DontSave 不会传递到组件，需单独设置
            // 同步原始 Tilemap 的 tileAnchor（场景中配置为 (0,0,0) 左下角），
            // 否则运行时创建的 Tilemap 默认 anchor 为 (0.5,0.5,0) 中心，导致半格偏移。
            tm.tileAnchor = this.tilemap.tileAnchor;
            TilemapRenderer tmr = chunkGO.AddComponent<TilemapRenderer>();
            tmr.hideFlags = HideFlags.DontSaveInEditor;
            // TilemapCollider2D 设 usedByComposite=true，几何体合并到 CompositeCollider2D，
            // 退出时各 Chunk Collider 本身轻量，不会造成序列化卡死。
            TilemapCollider2D tc = chunkGO.AddComponent<TilemapCollider2D>();
            tc.hideFlags = HideFlags.DontSaveInEditor;
            tc.usedByComposite = true;
            tc.enabled = false; // 地图生成完毕后再统一启用

            // 复制原始渲染器配置
            if (this.chunkMaterial != null)
            {
                tmr.material = this.chunkMaterial;
            }

            tmr.sortingLayerID = this.chunkSortingLayerID;
            tmr.sortingOrder = this.chunkSortingOrder;

            this.chunkTilemaps[chunkIndex] = tm;
            return tm;
        }

        /// <summary>
        /// 带缓存的 Chunk 获取 — 用于 ShowTilemap 顺序遍历时加速。
        /// </summary>
        private Tilemap GetChunkForPos(int x, int y)
        {
            Vector2Int idx = GetChunkIndex(x, y);
            if (idx == this.cachedChunkIndex && this.cachedChunkTilemap != null)
            {
                return this.cachedChunkTilemap;
            }

            this.cachedChunkIndex = idx;
            this.cachedChunkTilemap = this.GetOrCreateChunk(idx);
            return this.cachedChunkTilemap;
        }

        /// <summary>
        /// 销毁所有 Chunk GameObject 并清理缓存。
        /// </summary>
        private void ClearAllChunks()
        {
            foreach (Tilemap tm in this.chunkTilemaps.Values)
            {
                if (tm != null && tm.gameObject != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(tm.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(tm.gameObject);
                    }
                }
            }

            this.chunkTilemaps.Clear();
            this.cachedChunkIndex = new Vector2Int(int.MinValue, int.MinValue);
            this.cachedChunkTilemap = null;
        }

        /// <summary>
        /// 直接在指定 Chunk 的 local 位置设置幽灵瓦片（不触发递归边界同步）。
        /// 仅当该 Chunk 已存在时才执行。幽灵瓦片颜色设为全透明。
        /// </summary>
        private void SetTileDirect(Vector2Int chunkIndex, Vector3Int localPos, TileBase tileBase)
        {
            if (this.chunkTilemaps.TryGetValue(chunkIndex, out Tilemap chunk))
            {
                if (tileBase != null)
                {
                    chunk.SetTile(localPos, tileBase);
                    chunk.SetColor(localPos, GhostTransparentColor);
                }
                else
                {
                    chunk.SetTile(localPos, null);
                }
            }
        }

        /// <summary>
        /// 将 Chunk 边界的瓦片同步到相邻 Chunk 的幽灵位置。
        /// 双向同步：当前 Chunk 的幽灵位置（从相邻 Chunk 读取源瓦片），
        /// 以及相邻 Chunk 的幽灵位置（从当前瓦片复制）。
        /// </summary>
        private void SyncBorderToNeighbor(Vector2Int chunkIndex, Vector3Int localPos, TileBase tileBase)
        {
            int cx = chunkIndex.x;
            int cy = chunkIndex.y;
            int lx = localPos.x;
            int ly = localPos.y;

            // === 四条边：双向幽灵同步 ===
            if (lx == 0)
            {
                // 1) 当前 Chunk 的 (-1, ly) ← 来自 (cx-1, cy) 的 (63, ly)
                TileBase neighborTile = this.TryGetTileFromChunk(cx - 1, cy, chunkSize - 1, ly);
                if (neighborTile != null)
                {
                    this.SetTileDirect(chunkIndex, new Vector3Int(-1, ly, 0), neighborTile);
                }

                // 2) 相邻 Chunk (cx-1, cy) 的 (64, ly) ← 当前瓦片
                this.SetTileDirect(new Vector2Int(cx - 1, cy), new Vector3Int(chunkSize, ly, 0), tileBase);
            }

            if (lx == chunkSize - 1)
            {
                // 1) 当前 Chunk 的 (64, ly) ← 来自 (cx+1, cy) 的 (0, ly)
                TileBase neighborTile = this.TryGetTileFromChunk(cx + 1, cy, 0, ly);
                if (neighborTile != null)
                {
                    this.SetTileDirect(chunkIndex, new Vector3Int(chunkSize, ly, 0), neighborTile);
                }

                // 2) 相邻 Chunk (cx+1, cy) 的 (-1, ly) ← 当前瓦片
                this.SetTileDirect(new Vector2Int(cx + 1, cy), new Vector3Int(-1, ly, 0), tileBase);
            }

            if (ly == 0)
            {
                TileBase neighborTile = this.TryGetTileFromChunk(cx, cy - 1, lx, chunkSize - 1);
                if (neighborTile != null)
                {
                    this.SetTileDirect(chunkIndex, new Vector3Int(lx, -1, 0), neighborTile);
                }

                this.SetTileDirect(new Vector2Int(cx, cy - 1), new Vector3Int(lx, chunkSize, 0), tileBase);
            }

            if (ly == chunkSize - 1)
            {
                TileBase neighborTile = this.TryGetTileFromChunk(cx, cy + 1, lx, 0);
                if (neighborTile != null)
                {
                    this.SetTileDirect(chunkIndex, new Vector3Int(lx, chunkSize, 0), neighborTile);
                }

                this.SetTileDirect(new Vector2Int(cx, cy + 1), new Vector3Int(lx, -1, 0), tileBase);
            }

            // === 四个角 ===
            if (lx == 0 && ly == 0)
            {
                TileBase nt = this.TryGetTileFromChunk(cx - 1, cy - 1, chunkSize - 1, chunkSize - 1);
                if (nt != null) this.SetTileDirect(chunkIndex, new Vector3Int(-1, -1, 0), nt);
                this.SetTileDirect(new Vector2Int(cx - 1, cy - 1), new Vector3Int(chunkSize, chunkSize, 0), tileBase);
            }

            if (lx == chunkSize - 1 && ly == 0)
            {
                TileBase nt = this.TryGetTileFromChunk(cx + 1, cy - 1, 0, chunkSize - 1);
                if (nt != null) this.SetTileDirect(chunkIndex, new Vector3Int(chunkSize, -1, 0), nt);
                this.SetTileDirect(new Vector2Int(cx + 1, cy - 1), new Vector3Int(-1, chunkSize, 0), tileBase);
            }

            if (lx == 0 && ly == chunkSize - 1)
            {
                TileBase nt = this.TryGetTileFromChunk(cx - 1, cy + 1, chunkSize - 1, 0);
                if (nt != null) this.SetTileDirect(chunkIndex, new Vector3Int(-1, chunkSize, 0), nt);
                this.SetTileDirect(new Vector2Int(cx - 1, cy + 1), new Vector3Int(chunkSize, -1, 0), tileBase);
            }

            if (lx == chunkSize - 1 && ly == chunkSize - 1)
            {
                TileBase nt = this.TryGetTileFromChunk(cx + 1, cy + 1, 0, 0);
                if (nt != null) this.SetTileDirect(chunkIndex, new Vector3Int(chunkSize, chunkSize, 0), nt);
                this.SetTileDirect(new Vector2Int(cx + 1, cy + 1), new Vector3Int(-1, -1, 0), tileBase);
            }
        }

        /// <summary>
        /// 尝试从指定 Chunk 的 local 位置读取瓦片。
        /// </summary>
        private TileBase TryGetTileFromChunk(int cx, int cy, int lx, int ly)
        {
            if (this.chunkTilemaps.TryGetValue(new Vector2Int(cx, cy), out Tilemap chunk))
            {
                return chunk.GetTile(new Vector3Int(lx, ly, 0));
            }

            return null;
        }

        /// <summary>
        /// 全面同步所有 Chunk 边界：将每个 Chunk 的 4 条边 + 4 个角的瓦片
        /// 复制为相邻 Chunk 的幽灵瓦片，然后 RefreshTile 所有边界瓦片
        /// 使 Rule Tile 能跨 Chunk 重新评估。
        ///
        /// Phase 2 的 RefreshTile 会触发 Rule Tile 邻居评估，在 chunkSize 较大时
        /// 耗时较长，因此按 Chunk 分帧 yield 避免卡死。
        /// </summary>
        private IEnumerator SyncAllChunkBordersCoroutine()
        {
            // Phase 1: 为每个 Chunk 的边界放置幽灵瓦片（只读 + SetTile，较快）
            foreach (var kv in this.chunkTilemaps)
            {
                Vector2Int ci = kv.Key;
                Tilemap tm = kv.Value;
                int cx = ci.x;
                int cy = ci.y;

                // 左/右边缘
                for (int i = 0; i < chunkSize; i++)
                {
                    this.SyncBorderToNeighbor(ci, new Vector3Int(0, i, 0), tm.GetTile(new Vector3Int(0, i, 0)));
                    this.SyncBorderToNeighbor(ci, new Vector3Int(chunkSize - 1, i, 0), tm.GetTile(new Vector3Int(chunkSize - 1, i, 0)));
                }

                // 下/上边缘（跳过四角避免重复）
                for (int i = 1; i < chunkSize - 1; i++)
                {
                    this.SyncBorderToNeighbor(ci, new Vector3Int(i, 0, 0), tm.GetTile(new Vector3Int(i, 0, 0)));
                    this.SyncBorderToNeighbor(ci, new Vector3Int(i, chunkSize - 1, 0), tm.GetTile(new Vector3Int(i, chunkSize - 1, 0)));
                }
            }

            // Phase 2: 刷新所有 Chunk 的边界瓦片，使 Rule Tile 利用幽灵邻居重新评估。
            // RefreshTile 触发 Rule Tile 邻居评估，每个 Chunk 边界有 ~4*chunkSize 个瓦片需要刷新。
            // 在 chunkSize=250 时每 Chunk 约 1000 次刷新，按 Chunk 分帧避免卡死。
            int chunkCount = 0;
            foreach (var kv in this.chunkTilemaps)
            {
                Tilemap tm = kv.Value;

                for (int i = 0; i < chunkSize; i++)
                {
                    tm.RefreshTile(new Vector3Int(0, i, 0));
                    tm.RefreshTile(new Vector3Int(chunkSize - 1, i, 0));
                    tm.RefreshTile(new Vector3Int(i, 0, 0));
                    tm.RefreshTile(new Vector3Int(i, chunkSize - 1, 0));
                }

                // 每处理完 4 个 Chunk 的 RefreshTile 就 yield 一帧
                if (++chunkCount % 4 == 0)
                {
                    yield return null;
                }
            }
        }

        /// <summary>
        /// 每帧处理的瓦片数 — 与总 Chunk 数成反比。
        /// 16384 / (MaxChunksPerDim²)：
        ///   1 chunk  → 16384 tiles/frame
        ///   4 chunks →  4096 tiles/frame
        ///  16 chunks →  1024 tiles/frame
        /// </summary>
        private const int TILES_PER_YIELD = 16384 / (MaxChunksPerDim * MaxChunksPerDim);

        /// <summary>
        /// 显示地图 — 通过 TerrainConfigDatabase 查找每个瓦片的资源名。
        /// </summary>
        /// <param name="mapTiles">地形 ID 二维数组。</param>
        /// <returns>迭代器</returns>
        public IEnumerator ShowTilemap(int[,] mapTiles)
        {
            Core.GameServices.AsyncProgressSetTipProvider("正在展示地图...");

            // 清理旧 Chunk，确保重新渲染时状态干净
            this.ClearAllChunks();

            TerrainConfigDatabase db = ServiceLocator.Get<TerrainConfigDatabase>();
            int height = mapTiles.GetLength(0);
            int width = mapTiles.GetLength(1);

            // 根据地图尺寸动态调整 chunkSize，保证最多 MaxChunksPerDim² 个 Chunk
            this.ComputeChunkSize(height, width);

            AWorkerTask.LogProvider(
                $"[TileMap] 地图={width}×{height}, chunkSize={chunkSize}, chunks={MaxChunksPerDim}², TILES_PER_YIELD={TILES_PER_YIELD}",
                LogManager.LogLevelEnum.Info);

            // 缓存已加载的 TileBase，避免重复调用 ResourceLoadProvider（地形类型通常在 10 种以内）
            var tileCache = new Dictionary<string, TileBase>();

            int tilesProcessed = 0;
            int tilesSet = 0;
            int frameCount = 0;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    Core.GameServices.AsyncProgressAddOneProvider();
                    int terrainId = mapTiles[i, j];
                    string resourceName = db.GetTileResourceName(terrainId);
                    if (!string.IsNullOrEmpty(resourceName) && terrainId != 0)
                    {
                        if (!tileCache.TryGetValue(resourceName, out TileBase tile))
                        {
                            tile = (TileBase)AWorkerTask.ResourceLoadProvider(resourceName);
                            tileCache[resourceName] = tile;
                            AWorkerTask.LogProvider(
                                $"[TileMap] 加载资源 {tileCache.Count}: {resourceName}",
                                LogManager.LogLevelEnum.Info);
                        }

                        int cx = Mathf.FloorToInt((float)i / chunkSize);
                        int cy = Mathf.FloorToInt((float)j / chunkSize);
                        Vector3Int localPos = GetLocalPos(i, j, cx, cy);
                        this.GetChunkForPos(i, j).SetTile(localPos, tile);
                        tilesSet++;
                    }

                    if (++tilesProcessed % TILES_PER_YIELD == 0)
                    {
                        yield return null;
                        frameCount++;
                    }
                }
            }

            AWorkerTask.LogProvider(
                $"[TileMap] 瓦片设置完毕: 总数={tilesProcessed}, 有效={tilesSet}, 分{frameCount}帧, 资源类型={tileCache.Count}",
                LogManager.LogLevelEnum.Info);

            // 同步所有 Chunk 边界幽灵瓦片，使 Rule Tile 能跨 Chunk 查询邻居
            yield return this.SyncAllChunkBordersCoroutine();

            // 所有处理完成后，最后统一启用碰撞体
            this.EnableAllChunkColliders();
            AWorkerTask.LogProvider("[TileMap] Collider 已启用", LogManager.LogLevelEnum.Info);

            WalkabilityCache.Invalidate();
        }

        /// <summary>
        /// 统一启用所有 Chunk 的 TilemapCollider2D — 在所有瓦片和边界同步完成后调用。
        /// </summary>
        private void EnableAllChunkColliders()
        {
            foreach (Tilemap tm in this.chunkTilemaps.Values)
            {
                if (tm != null)
                {
                    var c = tm.GetComponent<TilemapCollider2D>();
                    if (c != null)
                    {
                        c.enabled = true;
                    }
                }
            }
        }

        /// <summary>
        /// 最大重试次数，防止无限循环
        /// </summary>
        private const int GEN_POS_MAX_RETRIES = 10000;

        /// <summary>
        /// 生成可用的位置，返回数组下标
        /// 可以选择以哪个点为中心，不选择则为所有
        /// 包括TileMap,ResourceMap,BuildMap可达位置
        /// </summary>
        /// <param name="centerMap">中心位置</param>
        /// <returns>位置</returns>
        public Vector3Int GenCanReachPos(Vector3 centerMap = default)
        {
            if (this.TileMapDataLAB == null)
            {
                AWorkerTask.LogProvider("TileMapDataLAB is null, cannot generate reachable position", LogManager.LogLevelEnum.Error);
                return Vector3Int.zero;
            }

            int x, y;
            int height = this.TileMapDataLAB.Height;
            int width = this.TileMapDataLAB.Width;
            int startX = 0, endX = height, startY = 0, endY = width;
            if (centerMap != default)
            {
                // clamp 中心点到地图内：centerMap 可能来自越界角色位置，
                // 若 startX > endX，Random.Range 会生成越界坐标（历史 bug 来源之一）。
                int roundX = Mathf.RoundToInt(centerMap.x);
                int roundY = Mathf.RoundToInt(centerMap.y);
                int cx = Mathf.Clamp(roundX, 0, Mathf.Max(0, height - 1));
                int cy = Mathf.Clamp(roundY, 0, Mathf.Max(0, width - 1));
                if (cx != roundX || cy != roundY)
                {
                    // 坐标越界兜底触发（历史 bug：地图外坐标导致睡眠死循环）→ 记录 clamp 前后坐标。
                    AWorkerTask.LogProvider(
                        $"[MapDiag] GenCanReachPos clamp 触发 center=({roundX},{roundY}) -> ({cx},{cy}) map={height}x{width}",
                        LogManager.LogLevelEnum.Debug);
                }

                startX = System.Math.Max(cx - 20, 0);
                startY = System.Math.Max(cy - 20, 0);
                endX = System.Math.Min(cx + 20, height);
                endY = System.Math.Min(cy + 20, width);
            }

            int retries = 0;
            Vector3Int posMap;
            do
            {
                x = UnityEngine.Random.Range(startX, endX);
                y = UnityEngine.Random.Range(startY, endY);
                posMap = new Vector3Int(x, y, 0);
                retries++;
                if (retries > GEN_POS_MAX_RETRIES)
                {
                    AWorkerTask.LogProvider(
                        $"GenCanReachPos exceeded max retries ({GEN_POS_MAX_RETRIES}), returning fallback position",
                        LogManager.LogLevelEnum.Error);
                    return new Vector3Int(startX, startY, 0);
                }
            }
            while (!(this.IsCanReach(posMap) && Core.ServiceLocator.Get<ResourceMap>().IsCanReach(posMap) && Core.ServiceLocator.Get<BuildMap>().IsCanReach(posMap)));
            return new Vector3Int(x, y, 0);
        }

        /// <summary>
        /// 随机生成地图板块分布(未实例化)。
        /// 委托给 ITerrainGenerator 策略执行具体算法。
        ///
        /// 新流程：噪声岛屿遮罩 → 散布种子 → BFS 填充 → 渲染。
        /// 海洋（水格）自然包围岛屿，不再需要矩形 Mountain 边框。
        /// </summary>
        /// <returns>迭代器</returns>
        public IEnumerator Create()
        {
            int height = this.TileMapDataLAB.Height;
            int width = this.TileMapDataLAB.Width;
            int randomCount = this.TileMapDataLAB.RandomCount;

            // Step 1: 生成陆地/海洋遮罩（噪声 + 距离衰减）
            int[,] tiles = new int[height, width];
            yield return this.StartCoroutine(this.generator.GenerateLandMask(tiles, height, width));

            // 遮罩生成后统计陆地格数量，动态调整 Fill 的进度总量
            int landCellCount = this.CountLandCells(tiles);
            Core.GameServices.AsyncProgressAddTotalProvider(landCellCount);

            // Step 2: 仅在陆地上散布地形种子
            yield return this.StartCoroutine(this.generator.ScatterSeeds(tiles, randomCount, height, width));

            // Step 3: BFS 并行填充陆地空白区域
            yield return this.StartCoroutine(this.generator.Fill(tiles, height, width));

            // Step 4: 清理孤立陆地碎块（BFS 无法到达的小斑块 → 转为水格）
            //         确保进度总量与实际处理量完全一致
            yield return this.StartCoroutine(this.CleanupUnfilledCells(tiles));

            this.TileMapDataLAB.MapTiles = tiles;
            WalkabilityCache.Invalidate();
            // 海洋即边界，不再调用 CreateArroundTile
            yield return this.StartCoroutine(this.ShowTilemap(this.TileMapDataLAB.MapTiles));
            Core.ServiceLocator.Get<MapInitCoordinator>().IsComplete = true;
        }

        /// <summary>
        /// 统计 tiles 中的陆地格数量（值不为水格 ID 的格子）。
        /// </summary>
        private int CountLandCells(int[,] tiles)
        {
            int count = 0;
            int h = tiles.GetLength(0);
            int w = tiles.GetLength(1);
            for (int x = 0; x < h; x++)
            {
                for (int y = 0; y < w; y++)
                {
                    if (tiles[x, y] != this.cachedWaterTerrainId)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// 清理 BFS 填充后残留的 0 格（孤立陆地碎块，无种子可达）。
        /// 将它们转为水格并上报进度，确保进度总量精确匹配。
        /// </summary>
        private IEnumerator CleanupUnfilledCells(int[,] tiles)
        {
            int h = tiles.GetLength(0);
            int w = tiles.GetLength(1);
            FrameControl frameControl = ServiceLocator.Get<FrameControl>();
            int batchCount = 0;

            for (int x = 0; x < h; x++)
            {
                for (int y = 0; y < w; y++)
                {
                    if (tiles[x, y] == 0)
                    {
                        tiles[x, y] = this.cachedWaterTerrainId;
                        Core.GameServices.AsyncProgressAddOneProvider();
                        batchCount++;
                    }

                    if (batchCount >= 5000 && frameControl.IsNeedStop(1))
                    {
                        batchCount = 0;
                        yield return null;
                    }
                }
            }
        }

        /// <summary>
        /// 地图坐标转世界坐标
        /// </summary>
        public Vector3 MapPosToWorldPos(Vector3Int posMap)
        {
            return new Vector3(posMap.y, posMap.x, 0);
        }

        /// <summary>
        /// 地图坐标转世界坐标
        /// </summary>
        public Vector3 MapPosToWorldPos(Vector3IntLAB posMap)
        {
            return new Vector3(posMap.Y, posMap.X, 0);
        }

        /// <summary>
        /// 地图坐标转世界坐标
        /// </summary>
        public Vector3 MapPosToWorldPos(Vector2ShortLAB posMap)
        {
            return new Vector3(posMap.Y, posMap.X, 0);
        }

        /// <summary>
        /// 世界坐标转地图坐标
        /// </summary>
        public Vector3Int WorldPosToMapPos(Vector3 worldPos)
        {
            return new Vector3Int(MathHelper.RoundToInt(worldPos.y), MathHelper.RoundToInt(worldPos.x), 0);
        }

        // === ITileMapQuery 接口实现 ===

        /// <inheritdoc/>
        GameGridPosition ITileMapQuery.WorldPosToMapPos(GameVector2 worldPos)
        {
            Vector3 unityPos = new Vector3(worldPos.X, worldPos.Y, 0);
            Vector3Int mapPos = this.WorldPosToMapPos(unityPos);
            return new GameGridPosition(mapPos.x, mapPos.y);
        }

        /// <inheritdoc/>
        bool ITileMapQuery.IsCanReach(GameGridPosition posMap)
        {
            return this.IsCanReach(new Vector3Int(posMap.X, posMap.Y, 0));
        }

        /// <inheritdoc/>
        int ITileMapQuery.Width => this.TileMapDataLAB?.Width ?? 0;

        /// <inheritdoc/>
        int ITileMapQuery.Height => this.TileMapDataLAB?.Height ?? 0;

        /// <inheritdoc/>
        bool ITileMapQuery.IsInBounds(GameGridPosition posMap)
        {
            if (posMap.X < 0 || posMap.X >= this.TileMapDataLAB?.Height
                || posMap.Y < 0 || posMap.Y >= this.TileMapDataLAB?.Width)
            {
                return false;
            }

            // 水域格视为界外（使用缓存 ID 避免频繁查找）
            if (this.TileMapDataLAB?.MapTiles != null && this.cachedWaterTerrainId > 0)
            {
                if (this.TileMapDataLAB.MapTiles[posMap.X, posMap.Y] == this.cachedWaterTerrainId)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 获取鼠标位置
        /// </summary>
        public Vector3Int GetMapPosByMouse()
        {
            return this.WorldPosToMapPos(UnityGlobalInputAdapter.GetMouseWorldPosition(Camera.main));
        }

        /// <summary>
        /// 地图索引是否越界。
        /// 数组越界或位于水域格（海洋）均视为越界。
        /// 使用缓存的 waterTerrainId 避免频繁 ServiceLocator 查找。
        /// </summary>
        public bool IsOverBorder(Vector3Int posMap)
        {
            if (posMap.x < 0 || posMap.x >= this.TileMapDataLAB.Height
                || posMap.y < 0 || posMap.y >= this.TileMapDataLAB.Width)
            {
                return true;
            }

            // 水域格（海洋）视为边界外
            if (this.TileMapDataLAB.MapTiles != null && this.cachedWaterTerrainId > 0)
            {
                if (this.TileMapDataLAB.MapTiles[posMap.x, posMap.y] == this.cachedWaterTerrainId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public override void SetTile(Vector3Int pos, TileBase tileBase)
        {
            Vector2Int chunkIndex = GetChunkIndex(pos.x, pos.y);
            Vector3Int localPos = GetLocalPos(pos.x, pos.y, chunkIndex.x, chunkIndex.y);
            Tilemap chunk = this.GetOrCreateChunk(chunkIndex);
            chunk.SetTile(localPos, tileBase);

            // 若瓦片位于 Chunk 边界，同步幽灵瓦片到相邻 Chunk
            int lx = localPos.x;
            int ly = localPos.y;
            if (lx == 0 || lx == chunkSize - 1 || ly == 0 || ly == chunkSize - 1)
            {
                this.SyncBorderToNeighbor(chunkIndex, localPos, tileBase);
            }
        }

        /// <inheritdoc/>
        public override TileBase GetTile(Vector3Int pos)
        {
            Vector2Int chunkIndex = GetChunkIndex(pos.x, pos.y);
            if (this.chunkTilemaps.TryGetValue(chunkIndex, out Tilemap chunk))
            {
                Vector3Int localPos = GetLocalPos(pos.x, pos.y, chunkIndex.x, chunkIndex.y);
                return chunk.GetTile(localPos);
            }

            return null;
        }

        /// <inheritdoc/>
        public override bool IsFreeTile(Vector3Int posMap)
        {
            return this.GetTile(posMap) == null;
        }

        /// <inheritdoc/>
        public override bool HasTile(Vector3Int pos)
        {
            return this.GetTile(pos) != null;
        }

        /// <inheritdoc/>
        public override bool IsCanReach(Vector3Int posMap)
        {
            Vector2Int chunkIndex = GetChunkIndex(posMap.x, posMap.y);
            if (this.chunkTilemaps.TryGetValue(chunkIndex, out Tilemap chunk))
            {
                Vector3Int localPos = GetLocalPos(posMap.x, posMap.y, chunkIndex.x, chunkIndex.y);
                return chunk.GetColliderType(localPos) == Tile.ColliderType.None;
            }

            // 没有 Chunk = 地图外 = 不可到达。
            // 地图边界外的格子不可被当作可通行，否则寻路/生成可达点会越出地图，
            // 导致角色被引导到地图外后陷入死循环（无法回家/无法睡觉）。
            AWorkerTask.LogProvider(
                $"[MapDiag] IsCanReach 越界 pos=({posMap.x},{posMap.y}) chunk=({chunkIndex.x},{chunkIndex.y}) 地图外=不可到达",
                LogManager.LogLevelEnum.Debug);
            return false;
        }

        /// <summary>
        /// 挖掘指定位置的地形瓦片（如山），将其替换为8邻域中最常见的可行走地形。
        /// </summary>
        /// <param name="posMap">要挖掘的地图位置</param>
        /// <returns>替换后的地形 ID，失败返回 0</returns>
        public int DigTerrain(Vector3Int posMap)
        {
            if (this.TileMapDataLAB?.MapTiles == null)
            {
                AWorkerTask.LogProvider("DigTerrain: TileMapData is null", LogManager.LogLevelEnum.Error);
                return 0;
            }

            int oldTerrainId = this.TileMapDataLAB.MapTiles[posMap.x, posMap.y];
            int newTerrainId = this.FindMostCommonWalkableNeighbor(posMap);

            // 更新地图数据
            this.TileMapDataLAB.MapTiles[posMap.x, posMap.y] = newTerrainId;

            // 更新视觉瓦片
            TerrainConfigDatabase db = ServiceLocator.Get<TerrainConfigDatabase>();
            string resourceName = db.GetTileResourceName(newTerrainId);
            if (!string.IsNullOrEmpty(resourceName))
            {
                this.SetTile(
                    new Vector3Int(posMap.x, posMap.y, 0),
                    (TileBase)AWorkerTask.ResourceLoadProvider(resourceName));
            }

            // 更新寻路缓存
            WalkabilityCache.UpdateCell(posMap);

            // 网络同步
            this.SyncSender.Broadcast(
                "SyncTerrainChange",
                DataTool.ToByteArray(Vector3IntLAB.ToVector3IntLAB(posMap)),
                newTerrainId);

            AWorkerTask.LogProvider(
                $"DigTerrain: pos=({posMap.x},{posMap.y}) terrainId {oldTerrainId} -> {newTerrainId}",
                LogManager.LogLevelEnum.Trace);

            return newTerrainId;
        }

        /// <summary>
        /// 查找指定位置8邻域中最常见的可行走地形 ID。
        /// </summary>
        private int FindMostCommonWalkableNeighbor(Vector3Int posMap)
        {
            TerrainConfigDatabase db = ServiceLocator.Get<TerrainConfigDatabase>();
            Dictionary<int, int> frequency = new Dictionary<int, int>();

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    int nx = posMap.x + dx;
                    int ny = posMap.y + dy;

                    if (nx < 0 || nx >= this.TileMapDataLAB.Height
                        || ny < 0 || ny >= this.TileMapDataLAB.Width)
                    {
                        continue;
                    }

                    int neighborId = this.TileMapDataLAB.MapTiles[nx, ny];

                    // 跳过水域
                    if (neighborId == this.cachedWaterTerrainId)
                    {
                        continue;
                    }

                    // 只统计可行走的地形
                    if (!db.IsWalkable(neighborId))
                    {
                        continue;
                    }

                    frequency.TryGetValue(neighborId, out int count);
                    frequency[neighborId] = count + 1;
                }
            }

            // 找出频率最高的
            if (frequency.Count > 0)
            {
                int bestId = 0;
                int bestCount = -1;
                foreach (KeyValuePair<int, int> kv in frequency)
                {
                    if (kv.Value > bestCount)
                    {
                        bestCount = kv.Value;
                        bestId = kv.Key;
                    }
                }

                return bestId;
            }

            // 回退：无可行走邻居 → 使用全局最高权重的可行走地形
            AWorkerTask.LogProvider(
                $"DigTerrain: pos=({posMap.x},{posMap.y}) 周围无可通行邻居，使用回退地形",
                LogManager.LogLevelEnum.Warning);
            return this.GetFallbackWalkableTerrainId(db);
        }

        /// <summary>
        /// 获取回退可行走地形 ID（无可行走邻居时使用）。
        /// 遍历所有地形配置，返回 spawnWeight 最高的可行走地形；仍找不到则返回 1（草地）。
        /// </summary>
        private int GetFallbackWalkableTerrainId(TerrainConfigDatabase db)
        {
            int bestId = 1;
            float bestWeight = -1f;

            foreach (int id in db.SpawnableIds)
            {
                if (db.IsWalkable(id))
                {
                    TerrainTileConfig config = db.GetById(id);
                    if (config != null && config.spawnWeight > bestWeight)
                    {
                        bestWeight = config.spawnWeight;
                        bestId = id;
                    }
                }
            }

            return bestId;
        }

        /// <summary>
        /// 接收来自其他客户端的地形变更同步（单格）。
        /// </summary>
        /// <param name="vector3IntLABBytes">位置</param>
        /// <param name="newTerrainId">新的地形 ID</param>
        [PunRPC]
        public void SyncTerrainChange(byte[] vector3IntLABBytes, int newTerrainId)
        {
            AWorkerTask.LogProvider("Response: 同步地形变更", LogManager.LogLevelEnum.Trace);
            Vector3Int pos = Vector3IntLAB.ToVector3Int(DataTool.FromByteArray<Vector3IntLAB>(vector3IntLABBytes));

            if (pos.x < 0 || pos.x >= this.TileMapDataLAB.Height
                || pos.y < 0 || pos.y >= this.TileMapDataLAB.Width)
            {
                return;
            }

            this.TileMapDataLAB.MapTiles[pos.x, pos.y] = newTerrainId;

            TerrainConfigDatabase db = ServiceLocator.Get<TerrainConfigDatabase>();
            string resourceName = db.GetTileResourceName(newTerrainId);
            if (!string.IsNullOrEmpty(resourceName))
            {
                this.SetTile(
                    new Vector3Int(pos.x, pos.y, 0),
                    (TileBase)AWorkerTask.ResourceLoadProvider(resourceName));
            }

            WalkabilityCache.UpdateCell(pos);
        }

        /// <summary>
        /// 设置进度（包含 TileMap 和 ResourceMap 的所有步骤）。
        /// </summary>
        public void SetProgress(int height, int width)
        {
            this.TileMapDataLAB = new TileMapData(height, width, new int[height, width], width * height / 2000);
            WalkabilityCache.Invalidate();
            int total = width * height;                       // GenerateLandMask 全格扫描
            total += this.TileMapDataLAB.RandomCount;         // ScatterSeeds 种子散布
            // Fill 的进度在遮罩生成后动态统计（只知道陆地格数才知道精确值）
            total += width * height;                          // ShowTilemap 渲染
            total += width * height;                          // ResourceMap.GenResource 资源生成
            Core.GameServices.AsyncProgressAddTotalProvider(total);
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            base.LoadData();
            Core.GameServices.AsyncProgressSetTipProvider("加载地图数据...");
            this.TileMapDataLAB = DataTool.LoadDataByBinary<TileMapData>(GlobalData.ConfigFile.GetPath(this.GetType().Name));
            WalkabilityCache.Invalidate();
            if (this.TileMapDataLAB == null)
            {
                AWorkerTask.LogProvider("TileMap data not found in archive, generating new default map", LogManager.LogLevelEnum.Warning);
                Core.ServiceLocator.Get<MapInitCoordinator>().IsComplete = false;
                this.SetProgress(DefaultHeight, DefaultWidth);
                this.StartCoroutine(this.Create());
                return;
            }

            Core.ServiceLocator.Get<MapInitCoordinator>().IsComplete = true;
            // 海洋即边界，不再需要矩形边框
            this.StartCoroutine(this.ShowTilemap(this.TileMapDataLAB.MapTiles));
        }

        /// <inheritdoc/>
        public override void SaveData()
        {
            base.SaveData();
            DataTool.SaveDataByBinary(GlobalData.ConfigFile.GetPath(this.GetType().Name), this.TileMapDataLAB);
        }

        /// <inheritdoc/>
        [PunRPC]
        public override void SyncDataReq(byte[] data)
        {
            base.SyncDataReq(data);
            AWorkerTask.LogProvider("Request: 同步地图数据", LogManager.LogLevelEnum.Trace);
            SyncDataTool.SyncDataRespWrapper(this.PhotonView, data, this.TileMapDataLAB);
        }

        /// <inheritdoc/>
        [PunRPC]
        public override void SyncDataResp(byte[] data)
        {
            base.SyncDataResp(data);
            AWorkerTask.LogProvider("Response: 同步地图数据", LogManager.LogLevelEnum.Trace);
            this.TileMapDataLAB = DataTool.FromByteArray<TileMapData>(data);
            WalkabilityCache.Invalidate();
            this.SetProgressAsync(this.TileMapDataLAB.MapTiles.GetLength(0), this.TileMapDataLAB.MapTiles.GetLength(1));
            this.StartCoroutine(this.ShowTilemap(this.TileMapDataLAB.MapTiles));
            // 海洋即边界，不再需要矩形边框
        }

        /// <summary>
        /// 同步数据设置进度（网络同步加载已有地图，只需渲染）。
        /// </summary>
        private void SetProgressAsync(int height, int width)
        {
            this.TileMapDataLAB.Height = height;
            this.TileMapDataLAB.Width = width;
            int total = width * height; // ShowTilemap 渲染
            Core.GameServices.AsyncProgressAddTotalProvider(total);
        }

        /// <summary>
        /// [已废弃] 地图四周创建矩形边界地形。
        /// 新流程使用噪声岛屿 + 海洋包围，不再需要矩形 Mountain 边框。
        /// </summary>
        [Obsolete("新流程使用海洋包围岛屿，不再需要矩形边框。保留以兼容旧存档。")]
        private void CreateArroundTile()
        {
            TerrainConfigDatabase db = ServiceLocator.Get<TerrainConfigDatabase>();
            int borderId = db.GetBorderTerrainId();
            string borderResourceName = db.GetTileResourceName(borderId);

            if (string.IsNullOrEmpty(borderResourceName))
            {
                AWorkerTask.LogProvider("CreateArroundTile: 没有配置边界地形（isBorder），跳过。", LogManager.LogLevelEnum.Warning);
                return;
            }

            Core.GameServices.AsyncProgressSetTipProvider("创建地图四周...");

            TileBase borderTile = (TileBase)AWorkerTask.ResourceLoadProvider(borderResourceName);

            // 上边
            for (int i = -1; i < this.TileMapDataLAB.Width; i++)
            {
                Core.GameServices.AsyncProgressAddOneProvider();
                this.tilemap.SetTile(new Vector3Int(this.TileMapDataLAB.Height, i, 0), borderTile);
            }

            // 右边
            for (int i = 0; i <= this.TileMapDataLAB.Height; i++)
            {
                Core.GameServices.AsyncProgressAddOneProvider();
                this.tilemap.SetTile(new Vector3Int(i, this.TileMapDataLAB.Width, 0), borderTile);
            }

            // 下边
            for (int i = 0; i <= this.TileMapDataLAB.Width; i++)
            {
                Core.GameServices.AsyncProgressAddOneProvider();
                this.tilemap.SetTile(new Vector3Int(-1, i, 0), borderTile);
            }

            // 左边
            for (int i = -1; i < this.TileMapDataLAB.Height; i++)
            {
                Core.GameServices.AsyncProgressAddOneProvider();
                this.tilemap.SetTile(new Vector3Int(i, -1, 0), borderTile);
            }
        }

        /// <summary>
        /// 瓦片数据 — 使用 int 存储地形 ID（map 到 TerrainTileConfig.terrainId）。
        /// </summary>
        [Serializable]
        public class TileMapData
        {
            public int Height;
            public int Width;

            /// <summary>
            /// 地图瓦片 — 每个值为地形 ID（0 = 未初始化/不渲染）。
            /// </summary>
            public int[,] MapTiles;

            public int RandomCount;

            public TileMapData(int height, int width, int[,] mapTiles, int randomCount)
            {
                this.Height = height;
                this.Width = width;
                this.MapTiles = mapTiles;
                this.RandomCount = randomCount;
            }
        }
    }
}
