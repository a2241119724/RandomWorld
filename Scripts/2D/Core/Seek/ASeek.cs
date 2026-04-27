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
        /// 邻居
        /// </summary>
        protected static readonly List<Vector2SByteLAB> Neighbors = new ()
        {
            new Vector2SByteLAB(0, 1),
            new Vector2SByteLAB(1, 0),
            new Vector2SByteLAB(0, -1),
            new Vector2SByteLAB(-1, 0), // 上右下左

            // new Vector2SByte(1, 1), new Vector2SByte(1, -1), // 右上,右下
            // new Vector2SByte(-1, -1), new Vector2SByte(-1, 1), // 左下, 左上
        };

        /// <summary>
        /// Value为空时，说明正在寻路
        /// </summary>
        protected static ConcurrentDictionary<string, SeekResult> results = new ();

        /// <summary>
        /// 合并path时检测射线偏移
        /// </summary>
        protected readonly Vector3[] checkOffsets = { new Vector3(0, 0), new Vector3(-0.5f, 0), new Vector3(0.5f, 0), new Vector3(0, 0.5f), new Vector3(0, -0.5f) };

        /// <summary>
        /// 地图中板块的花费
        /// TODO, 优化公共池
        /// </summary>
        protected volatile Spend[,] mapSpend;

        /// <summary>
        /// 控制线程停止
        /// </summary>
        protected volatile bool isStopThread = false;

        /// <summary>
        /// 待处理的板块
        /// </summary>
        protected volatile List<Spend> openList;

        /// <summary>
        /// 已处理的板块
        /// </summary>
        protected volatile List<Spend> closeList;

        // private static ManualResetEvent manualResetEvent; // 线程Wait
        public ASeek(Character character)
        {
            int height = TileMap.Instance.TileMapDataLAB.Height;
            int width = TileMap.Instance.TileMapDataLAB.Width;

            // 初始化寻路花费
            this.mapSpend = new Spend[height, width];
            for (short i = 0; i < height; i++)
            {
                for (short j = 0; j < width; j++)
                {
                    this.mapSpend[i, j] = new Spend(i, j);
                }
            }

            // 路径
            this.LineRenderer = character.GetComponent<LineRenderer>();
            this.LineRenderer.startWidth = 0.05f;
            this.LineRenderer.endWidth = 0.05f;
            Material material = new (Shader.Find("Unlit/Color"));
            material.color = new Color(UnityEngine.Random.Range(0.5f, 1.0f), UnityEngine.Random.Range(0.5f, 1.0f), UnityEngine.Random.Range(0.5f, 1.0f));
            this.LineRenderer.material = material;
            this.LineRenderer.sortingLayerName = "Highest";

            this.Character = character;
            this.openList = new List<Spend>();
            this.closeList = new List<Spend>();
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
                LogManager.Instance.Log(this.Character.name + ":重新寻路!");
            }

            this.StartSeek();

            // 线程内获取targetMap可能是上一次的值, 所以不传入targetMap
            ThreadPool.QueueUserWorkItem(t =>
            {
                // 之前的线程停止后执行
                lock (this)
                {
                    // if (TileMap.Instance.TileMapDataLAB.Height == 0 || TileMap.Instance.TileMapDataLAB.Width == 0)
                    // {
                    //     ASeek.manualResetEvent.WaitOne();
                    // }
                    this.isStopThread = false;
                    this.DoSeek();

                    // 显示路径
                    UnityMainThreadDispatcher.Instance.EnqueueAsync(() =>
                    {
                        this.UpdateLine();
                    }).Wait();
                }
            });
        }

        /// <summary>
        /// 停止移动
        /// </summary>
        public void StopMove()
        {
            ASeek.results.TryRemove(this.Character.CharacterDataLAB.SeekId, out SeekResult result);
            this.LineRenderer.positionCount = 0;
        }

        /// <summary>
        /// 根据路径移动
        /// </summary>
        /// <returns>是否到达目标</returns>
        public bool MoveByPath()
        {
            // 没有路径返回到达目标
            if (!ASeek.results.TryGetValue(this.Character.CharacterDataLAB.SeekId, out SeekResult result) || result == null)
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
            this.Character.transform.Translate(this.Character.MoveSpeed * Time.deltaTime * this.Direction.normalized, Space.World); // 向前移动
            this.UpdateLine(true);
            return false;
        }

        /// <summary>
        /// 是否正在寻路
        /// </summary>
        /// <returns>是否</returns>
        public bool IsSeeking()
        {
            if (ASeek.results.TryGetValue(this.Character.CharacterDataLAB.SeekId, out SeekResult result) && result == null)
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
            if (!ASeek.results.TryGetValue(this.Character.CharacterDataLAB.SeekId, out SeekResult result))
            {
                LogManager.Instance.Log(this.Character.name + ":获取寻路结果失败!", LogManager.LogLevelEnum.Warning);
                return false;
            }

            return result != null;
        }

        /// <summary>
        /// 寻路初始化
        /// </summary>
        public void StartSeek()
        {
            // 停止之前的线程
            this.isStopThread = true;
            this.openList.Clear();
            this.closeList.Clear();
            this.SeekProgress = 0.0f;
            ASeek.results.TryRemove(this.Character.CharacterDataLAB.SeekId, out SeekResult result);
            if (!ASeek.results.TryAdd(this.Character.CharacterDataLAB.GenerateSeekId(), null))
            {
                LogManager.Instance.Log(this.Character.name + ":添加寻路任务失败!", LogManager.LogLevelEnum.Warning);
            }

            this.UpdateLine();
        }

        /// <summary>
        /// 设置结果
        /// </summary>
        /// <param name="result">结果</param>
        public void SetResult(SeekResult result)
        {
            if (!ASeek.results.TryUpdate(this.Character.CharacterDataLAB.SeekId, result, null) && result.Path.Count != 0)
            {
                UnityMainThreadDispatcher.Instance.EnqueueAsync(() =>
                {
                    LogManager.Instance.Log(this.Character.name + this.Character.CharacterDataLAB.SeekId + "更新寻路结果失败!", LogManager.LogLevelEnum.Warning);
                }).Wait();
            }
        }

        protected virtual void DoSeek()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// 更新路径UI
        /// </summary>
        /// <param name="isFirst">是否仅更新第一段线</param>
        protected void UpdateLine(bool isFirst = false)
        {
            if (!ASeek.results.TryGetValue(this.Character.CharacterDataLAB.SeekId, out SeekResult result))
            {
                LogManager.Instance.Log(this.Character.name + ":获取寻路结果失败!", LogManager.LogLevelEnum.Warning);
            }

            if (result == null)
            {
                this.LineRenderer.positionCount = 0;
                return;
            }

            if (this.LineRenderer == null)
            {
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
            /// 寻路结果, 在主线程执行
            /// </summary>
            public List<Spend> Path { get; set; } = new ();
        }
    }
}
