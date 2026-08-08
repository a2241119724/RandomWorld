namespace LAB2D.Map
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Core;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 基于 Perlin 噪声的不规则岛屿地形生成器。
    ///
    /// 分三步生成地图：
    /// 1. GenerateLandMask — Perlin 噪声 + 径向距离衰减生成岛屿陆地/海洋遮罩
    /// 2. ScatterSeeds      — 仅在陆地格上按权重散布地形种子
    /// 3. Fill              — 并行 BFS 从所有种子扩展，仅填充陆地格（Voronoi 分区）
    ///
    /// 海洋格在步骤 1 中设为水域地形 ID，后续步骤自动跳过。
    /// </summary>
    public class RandomScatterFillGenerator : ITerrainGenerator
    {
        /// <summary>
        /// 一次 BFS 帧内处理的最大扩展操作数。
        /// </summary>
        private const int BFS_BATCH_SIZE = 2000;

        /// <summary>
        /// 岛屿遮罩的陆地判定阈值（0.0-1.0）。值越大岛屿越小。
        /// </summary>
        private const float LAND_THRESHOLD = 0.38f;

        // 4 邻域方向：上、下、左、右
        private static readonly int[] Dx = { -1, 1, 0, 0 };
        private static readonly int[] Dy = { 0, 0, -1, 1 };

        /// <summary>
        /// 生成陆地/水域遮罩 — Perlin 噪声 + 径向距离衰减。
        /// 水域格直接设为水域地形 ID，陆地格保持 0（待填充）。
        /// </summary>
        public IEnumerator GenerateLandMask(int[,] tiles, int height, int width)
        {
            Core.GameServices.AsyncProgressSetTipProvider("正在生成岛屿地形...");

            TerrainConfigDatabase db = ServiceLocator.Get<TerrainConfigDatabase>();
            int waterId = db.GetWaterTerrainId();
            FrameControl frameControl = ServiceLocator.Get<FrameControl>();

            for (int x = 0; x < height; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    Core.GameServices.AsyncProgressAddOneProvider();

                    float nx = (float)x / height;
                    float ny = (float)y / width;

                    // 3 层 Perlin 噪声（不同频率 + 相位偏移）
                    float noise =
                        Mathf.PerlinNoise(nx * 5.0f, ny * 5.0f) * 0.5f +
                        Mathf.PerlinNoise(nx * 10.0f + 3.7f, ny * 10.0f + 2.1f) * 0.35f +
                        Mathf.PerlinNoise(nx * 20.0f + 7.2f, ny * 20.0f + 5.9f) * 0.15f;

                    // 径向距离衰减（中心 1.0 → 边缘 0.0）
                    float dx = nx - 0.5f;
                    float dy = ny - 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / 0.5f;
                    float falloff = 1.0f - Mathf.Pow(Mathf.Clamp01(dist), 1.5f);

                    // 混合噪声与衰减
                    float landValue = noise * 0.55f + falloff * 0.45f;

                    if (landValue < LAND_THRESHOLD)
                    {
                        tiles[x, y] = waterId;
                    }
                    // else: 保持 0（陆地，待后续填充）
                }

                if (frameControl.IsNeedStop(1))
                {
                    yield return null;
                }
            }
        }

        /// <summary>
        /// 在陆地格上按权重散布地形种子。
        /// 预先收集所有陆地坐标，随机采样以避免拒绝采样的浪费。
        /// </summary>
        public IEnumerator ScatterSeeds(int[,] tiles, int randomCount, int height, int width)
        {
            Core.GameServices.AsyncProgressSetTipProvider("正在散布地形种子...");

            TerrainConfigDatabase db = ServiceLocator.Get<TerrainConfigDatabase>();
            int waterId = db.GetWaterTerrainId();
            FrameControl frameControl = ServiceLocator.Get<FrameControl>();

            // 预收集所有陆地格坐标
            List<Vector2Int> landCells = new List<Vector2Int>();
            for (int x = 0; x < height; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    if (tiles[x, y] != waterId)
                    {
                        landCells.Add(new Vector2Int(x, y));
                    }
                }
            }

            if (landCells.Count == 0)
            {
                AWorkerTask.LogProvider("ScatterSeeds: 没有陆地格，跳过种子散布。", LogManager.LogLevelEnum.Error);
                yield break;
            }

            // 从陆地格中随机采样放置种子
            int seedsPlaced = 0;
            int maxAttempts = randomCount * 3; // 安全上限，避免无限循环
            for (int i = 0; i < randomCount && seedsPlaced < maxAttempts; i++)
            {
                int index = Random.Range(0, landCells.Count);
                Vector2Int pos = landCells[index];
                if (tiles[pos.x, pos.y] == 0)
                {
                    tiles[pos.x, pos.y] = db.GetRandomWeighted();
                    seedsPlaced++;
                }

                Core.GameServices.AsyncProgressAddOneProvider();
                if (frameControl.IsNeedStop(1))
                {
                    yield return null;
                }
            }
        }

        /// <summary>
        /// 并行 BFS 填充 — 从所有种子点同时扩展，自然形成 Voronoi 分区。
        /// 复杂度 O(陆地格数)，比原来的逐格螺旋搜索 O(n×d²) 大幅优化。
        /// </summary>
        public IEnumerator Fill(int[,] tiles, int height, int width)
        {
            Core.GameServices.AsyncProgressSetTipProvider("正在填充地形...");

            TerrainConfigDatabase db = ServiceLocator.Get<TerrainConfigDatabase>();
            int waterId = db.GetWaterTerrainId();
            FrameControl frameControl = ServiceLocator.Get<FrameControl>();

            // 收集所有种子点入队
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            for (int x = 0; x < height; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    int val = tiles[x, y];
                    if (val != 0 && val != waterId)
                    {
                        queue.Enqueue(new Vector2Int(x, y));
                    }
                }
            }

            if (queue.Count == 0)
            {
                AWorkerTask.LogProvider("Fill: 没有种子点，跳过填充。", LogManager.LogLevelEnum.Error);
                yield break;
            }

            // BFS 扩展
            int batchCount = 0;
            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                int currentTerrain = tiles[current.x, current.y];

                for (int d = 0; d < 4; d++)
                {
                    int nx = current.x + Dx[d];
                    int ny = current.y + Dy[d];

                    // 越界检查
                    if (nx < 0 || nx >= height || ny < 0 || ny >= width)
                    {
                        continue;
                    }

                    // 只填充空地（值为 0），跳过水域和已填充格
                    if (tiles[nx, ny] != 0)
                    {
                        continue;
                    }

                    tiles[nx, ny] = currentTerrain;
                    queue.Enqueue(new Vector2Int(nx, ny));
                }

                Core.GameServices.AsyncProgressAddOneProvider();

                batchCount++;
                if (batchCount >= BFS_BATCH_SIZE)
                {
                    batchCount = 0;
                    if (frameControl.IsNeedStop(1))
                    {
                        yield return null;
                    }
                }
            }
        }
    }
}
