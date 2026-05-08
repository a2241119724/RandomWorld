namespace LAB2D
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using PimDeWitte.UnityMainThreadDispatcher;
    using UnityEngine;

    /// <summary>
    /// 寻路
    /// </summary>
    public class ASeek : ISeek
    {
        /// <summary>
        /// 同时寻路的最大并发数
        /// </summary>
        private static readonly SemaphoreSlim ConcurrencyLimit = new (Environment.ProcessorCount, Environment.ProcessorCount);

        /// <summary>
        /// 共享LineRenderer材质
        /// </summary>
        private static Material sharedLineMaterial;

        /// <summary>
        /// 邻居
        /// </summary>
        protected static readonly List<Vector2SByteLAB> Neighbors = new ()
        {
            new Vector2SByteLAB(0, 1),
            new Vector2SByteLAB(1, 0),
            new Vector2SByteLAB(0, -1),
            new Vector2SByteLAB(-1, 0), // 上右下左
        };

        /// <summary>
        /// Value为空时，说明正在寻路
        /// </summary>
        protected static ConcurrentDictionary<string, SeekResult> results = new ();

        /// <summary>
        /// 当前寻路任务的结果键。
        /// </summary>
        private string activeSeekId = string.Empty;

        /// <summary>
        /// 合并path时检测射线偏移
        /// </summary>
        protected readonly Vector3[] checkOffsets = { new Vector3(0, 0), new Vector3(-0.5f, 0), new Vector3(0.5f, 0), new Vector3(0, 0.5f), new Vector3(0, -0.5f) };

        /// <summary>
        /// 地图中板块的花费(使用共享池)
        /// </summary>
        protected Spend[,] mapSpend;

        /// <summary>
        /// 控制线程停止
        /// </summary>
        protected volatile bool isStopThread = false;

        /// <summary>
        /// 搜索代数, 每次StartSeek递增, 用于丢弃过时的搜索任务
        /// </summary>
        protected volatile int seekGeneration = 0;

        /// <summary>
        /// 待处理的板块
        /// </summary>
        protected List<Spend> openList;

        /// <summary>
        /// 已处理的板块
        /// </summary>
        protected List<Spend> closeList;

        public ASeek(Character character)
        {
            // 确保Spend池和可步行性缓存已初始化
            var tileMap = TileMap.Instance.TileMapDataLAB;
            SpendPool.Initialize(tileMap.Width, tileMap.Height);
            WalkabilityCache.Initialize(tileMap.Width, tileMap.Height);

            // 路径 — 使用共享材质模板, 每个实例用不同颜色
            this.LineRenderer = character.GetComponent<LineRenderer>();
            this.LineRenderer.startWidth = 0.05f;
            this.LineRenderer.endWidth = 0.05f;
            if (sharedLineMaterial == null)
            {
                sharedLineMaterial = new Material(Shader.Find("Unlit/Color"));
            }

            Material instanceMat = new (sharedLineMaterial);
            instanceMat.color = new Color(UnityEngine.Random.Range(0.5f, 1.0f), UnityEngine.Random.Range(0.5f, 1.0f), UnityEngine.Random.Range(0.5f, 1.0f));
            this.LineRenderer.material = instanceMat;
            this.LineRenderer.sortingLayerName = "Highest";

            this.Character = character;
            this.openList = new List<Spend>(128);
            this.closeList = new List<Spend>(128);
        }

        /// <summary>
        /// 目标地图坐标
        /// </summary>
        public Vector3Int TargetMap { get; protected set; }

        /// <summary>
        /// 寻路进度
        /// </summary>
        public float SeekProgress { get; protected set; }

        /// <summary>
        /// 寻路路径渲染
        /// </summary>
        public LineRenderer LineRenderer { get; set; }

        /// <summary>
        /// 移动的方向
        /// </summary>
        public Vector3 Direction { get; private set; }

        /// <summary>
        /// 寻路进度
        /// </summary>
        protected Character Character { get; set; }

        /// <summary>
        /// 是否可以抵达(不包含带有碰撞体的Tile,即使是正在建造中的)
        /// </summary>
        /// <param name="posMap">目标坐标</param>
        /// <returns>是否</returns>
        public static bool IsCanReach(Vector3Int posMap)
        {
            if (!TileMap.Instance.IsCanReach(posMap))
            {
                return false;
            }

            if (!ResourceMap.Instance.IsCanReach(posMap))
            {
                return false;
            }

            if (!BuildMap.Instance.IsCanReach(posMap))
            {
                return false;
            }

            return true;
        }

        public void Seek(Vector3Int targetMap)
        {
            this.TargetMap = targetMap;
            if (this.IsSeeking())
            {
                LogManager.Instance.Log(this.Character.name + ":重新寻路!", LogManager.LogLevelEnum.Trace);
            }

            string seekId = this.StartSeek();

            // 捕获当前代数, 用于在等待信号量后验证搜索是否已过期
            int capturedGeneration = this.seekGeneration;

            // 使用Task.Run以支持async/await并发控制
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                await ConcurrencyLimit.WaitAsync();
                try
                {
                    lock (this)
                    {
                        // 在等待信号量期间, 新的StartSeek可能已递增代数 — 丢弃过期任务
                        if (capturedGeneration != this.seekGeneration || !seekId.Equals(this.activeSeekId))
                        {
                            return;
                        }

                        this.isStopThread = false;

                        // 从池中租借Spend数组, 搜索完成后归还
                        this.mapSpend = SpendPool.Rent();
                        try
                        {
                            this.DoSeek(seekId);
                        }
                        finally
                        {
                            SpendPool.Return(this.mapSpend);
                            this.mapSpend = null;
                        }

                        // 显示路径(仅在未被新搜索打断时)
                        if (capturedGeneration == this.seekGeneration && seekId.Equals(this.activeSeekId))
                        {
                            UnityMainThreadDispatcher.Instance.EnqueueAsync(() =>
                            {
                                this.UpdateLine(seekId);
                            }).Wait();
                        }
                    }
                }
                finally
                {
                    ConcurrencyLimit.Release();
                }
            });
        }

        /// <summary>
        /// 停止移动
        /// </summary>
        public void StopMove()
        {
            if (!string.IsNullOrEmpty(this.activeSeekId))
            {
                ASeek.results.TryRemove(this.activeSeekId, out _);
                this.activeSeekId = string.Empty;
            }

            this.LineRenderer.positionCount = 0;
        }

        /// <summary>
        /// 根据路径移动
        /// </summary>
        /// <returns>是否到达目标</returns>
        public bool MoveByPath()
        {
            // 没有路径返回到达目标
            if (string.IsNullOrEmpty(this.activeSeekId) ||
                !ASeek.results.TryGetValue(this.activeSeekId, out SeekResult result) || result == null)
            {
                return true;
            }

            if (result.Path.Count == 0)
            {
                this.StopMove();
                return true;
            }

            // 变为真实坐标
            Vector3 worldPos = TileMap.Instance.MapPosToWorldPos(result.Path[0].PosMap);

            // 到达路径中一个目标点，切换下一个目标点
            if (result.Path.Count != 0 &&
                Mathf.Abs(worldPos.x - this.Character.transform.position.x) < 0.1f &&
                Mathf.Abs(worldPos.y - this.Character.transform.position.y) < 0.1f)
            {
                result.Path.RemoveAt(0); // --path.Count
            }

            this.Direction = worldPos - this.Character.transform.position;
            float speed = WeatherGameplayEffect.Instance.GetAdjustedCharacterMoveSpeed(this.Character, this.Character.MoveSpeed);
            this.Character.transform.Translate(speed * Time.deltaTime * this.Direction.normalized, Space.World); // 向前移动
            this.UpdateLine(true);
            return false;
        }

        /// <summary>
        /// 是否正在寻路
        /// </summary>
        /// <returns>是否</returns>
        public bool IsSeeking()
        {
            if (!string.IsNullOrEmpty(this.activeSeekId) &&
                ASeek.results.TryGetValue(this.activeSeekId, out SeekResult result) && result == null)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 寻路结束后是否有路径
        /// </summary>
        /// <returns>是否</returns>
        public bool IsHavePath()
        {
            if (string.IsNullOrEmpty(this.activeSeekId) ||
                !ASeek.results.TryGetValue(this.activeSeekId, out SeekResult result))
            {
                return false;
            }

            return result != null && result.IsReachable;
        }

        /// <summary>
        /// 寻路初始化(主线程调用)
        /// </summary>
        public string StartSeek()
        {
            // 停止之前的线程, 递增代数使旧搜索任务失效
            this.isStopThread = true;
            this.seekGeneration++;
            this.openList.Clear();
            this.closeList.Clear();
            this.SeekProgress = 0.0f;
            if (!string.IsNullOrEmpty(this.activeSeekId))
            {
                ASeek.results.TryRemove(this.activeSeekId, out _);
            }

            this.activeSeekId = this.Character.CharacterDataLAB.GenerateSeekId();
            if (!ASeek.results.TryAdd(this.activeSeekId, null))
            {
                LogManager.Instance.Log(this.Character.name + ":添加寻路任务失败!", LogManager.LogLevelEnum.Warning);
            }

            // 刷新可步行性缓存, 避免A*循环中每次邻居检查都向主线程派发
            WalkabilityCache.Refresh();

            this.UpdateLine();
            return this.activeSeekId;
        }

        /// <summary>
        /// 设置结果
        /// </summary>
        /// <param name="result">结果</param>
        /// <param name="seekId">寻路结果键</param>
        public void SetResult(SeekResult result, string seekId)
        {
            if (!ASeek.results.TryUpdate(seekId, result, null) && result.Path.Count != 0)
            {
                UnityMainThreadDispatcher.Instance.EnqueueAsync(() =>
                {
                    LogManager.Instance.Log(this.Character.name + seekId + "更新寻路结果失败!", LogManager.LogLevelEnum.Warning);
                }).Wait();
            }
        }

        protected virtual void DoSeek(string seekId)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// 更新路径UI
        /// </summary>
        /// <param name="isFirst">是否仅更新第一段线</param>
        protected void UpdateLine(bool isFirst = false)
        {
            this.UpdateLine(this.activeSeekId, isFirst);
        }

        /// <summary>
        /// 更新路径UI
        /// </summary>
        /// <param name="seekId">寻路结果键</param>
        /// <param name="isFirst">是否仅更新第一段线</param>
        protected void UpdateLine(string seekId, bool isFirst = false)
        {
            if (this.LineRenderer == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(seekId) || !ASeek.results.TryGetValue(seekId, out SeekResult result))
            {
                this.LineRenderer.positionCount = 0;
                return;
            }

            if (result == null || !result.IsReachable)
            {
                this.LineRenderer.positionCount = 0;
                return;
            }

            this.LineRenderer.positionCount = result.Path.Count + 1;
            this.LineRenderer.SetPosition(result.Path.Count, this.Character.transform.position);

            if (isFirst)
            {
                return;
            }

            for (int i = 0; i < result.Path.Count; i++)
            {
                this.LineRenderer.SetPosition(result.Path.Count - i - 1, TileMap.Instance.MapPosToWorldPos(result.Path[i].PosMap));
            }
        }

        /// <summary>
        /// 线程同步
        /// </summary>
        public class SeekResult
        {
            /// <summary>
            /// 是否可以抵达目标；起点等于终点时为true且路径为空。
            /// </summary>
            public bool IsReachable { get; set; } = true;

            /// <summary>
            /// 寻路结果, 在主线程执行
            /// </summary>
            public List<Spend> Path { get; set; } = new ();
        }
    }
}
