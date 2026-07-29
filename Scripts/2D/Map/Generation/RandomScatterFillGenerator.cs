namespace LAB2D.Map
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Core;
    using System.Collections;

    /// <summary>
    /// 随机散布 + 最近邻填充 地形生成器。
    /// 种子点通过 TerrainConfigDatabase.GetRandomWeighted() 按权重散布，
    /// 空白区域用螺旋搜索找到最近的非 Default 种子点填充。
    /// </summary>
    public class RandomScatterFillGenerator : ITerrainGenerator
    {
        /// <summary>
        /// 在随机位置散布地形种子点。
        /// 通过 TerrainConfigDatabase.GetRandomWeighted() 根据权重选择地形类型。
        /// </summary>
        public IEnumerator ScatterSeeds(int[,] tiles, int randomCount, int height, int width)
        {
            Core.GameServices.AsyncProgressSetTipProvider("正在生成随机坐标...");

            TerrainConfigDatabase db = ServiceLocator.Get<TerrainConfigDatabase>();

            for (int i = 0; i < randomCount; i++)
            {
                int x = UnityEngine.Random.Range(0, height);
                int y = UnityEngine.Random.Range(0, width);
                tiles[x, y] = db.GetRandomWeighted();

                Core.GameServices.AsyncProgressAddOneProvider();
                if (ServiceLocator.Get<FrameControl>().IsNeedStop(1))
                {
                    yield return null;
                }
            }
        }

        /// <summary>
        /// 将所有 Default（值为 0）的格子用距离最近的非 Default 种子点填充。
        /// 使用螺旋搜索算法（按曼哈顿距离扩展的菱形边界扫描）。
        /// </summary>
        public IEnumerator Fill(int[,] tiles, int height, int width)
        {
            Core.GameServices.AsyncProgressSetTipProvider("正在填补地图...");

            // 创建输出数组，同时保留源数据的副本用于读取
            int[,] source = (int[,])tiles.Clone();
            int[,] output = (int[,])tiles.Clone();

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    if (ServiceLocator.Get<FrameControl>().IsNeedStop(1))
                    {
                        yield return null;
                    }

                    Core.GameServices.AsyncProgressAddOneProvider();

                    if (source[i, j] != 0)
                    {
                        // 已是种子点，直接保留
                        continue;
                    }

                    // 寻找最近的非 Default 邻居并赋值
                    FillCell(source, output, i, j, height, width);
                }
            }

            // 将输出复制回 tiles
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    tiles[i, j] = output[i, j];
                }
            }
        }

        /// <summary>
        /// 以 (i, j) 为中心，按扩展螺旋搜索最近的非 Default 瓦片。
        /// </summary>
        private static void FillCell(int[,] source, int[,] output, int i, int j, int height, int width)
        {
            for (int t = 1; t < width; t++)
            {
                // 顶边
                int k = i - t;
                for (int l = j - t; l <= j + t; l++)
                {
                    if (IsValidAndNonDefault(source, k, l, height, width))
                    {
                        output[i, j] = source[k, l];
                        return;
                    }
                }

                // 左右两列
                for (++k; k < i + t; k++)
                {
                    int l = j - t;
                    if (IsValidAndNonDefault(source, k, l, height, width))
                    {
                        output[i, j] = source[k, l];
                        return;
                    }

                    l = j + t;
                    if (IsValidAndNonDefault(source, k, l, height, width))
                    {
                        output[i, j] = source[k, l];
                        return;
                    }
                }

                // 底边
                for (int l = j - t; l <= j + t; l++)
                {
                    if (IsValidAndNonDefault(source, k, l, height, width))
                    {
                        output[i, j] = source[k, l];
                        return;
                    }
                }
            }
        }

        private static bool IsValidAndNonDefault(int[,] source, int k, int l, int height, int width)
        {
            return k >= 0 && k < height && l >= 0 && l < width && source[k, l] != 0;
        }
    }
}
