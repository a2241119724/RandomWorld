namespace LAB2D
{
    using System.Collections.Generic;
    using System.Threading;
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
        /// 合并path时检测射线偏移
        /// </summary>
        protected readonly Vector3[] checkOffsets = { new Vector3(0, 0), new Vector3(-0.5f, 0), new Vector3(0.5f, 0), new Vector3(0, 0.5f), new Vector3(0, -0.5f) };

        /// <summary>
        /// 地图中板块的花费
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

        /// <summary>
        /// 寻路结果
        /// </summary>
        protected volatile List<Spend> path;

        // private static ManualResetEvent manualResetEvent; // 线程Wait
        public ASeek(Worker character)
        {
            int height = TileMap.Height;
            int width = TileMap.Width;

            // 初始化寻路花费
            this.mapSpend = new Spend[height, width];
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
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
            this.path = new List<Spend>();
        }

        /// <summary>
        /// 目标地图坐标
        /// </summary>
        public Vector3Int TargetMap { get; set; }

        /// <summary>
        /// 寻路进度
        /// </summary>
        public float SeekProgress { get; set; }

        /// <summary>
        /// 是否在寻路
        /// </summary>
        public bool IsSeeking { get; set; }

        /// <summary>
        /// 寻路路径渲染
        /// </summary>
        public LineRenderer LineRenderer { get; set; }

        /// <summary>
        /// 寻路进度
        /// </summary>
        protected Worker Character { get; set; }

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
            ThreadPool.QueueUserWorkItem(t =>
            {
                // 停止之前的线程
                this.isStopThread = true;

                // 之前的线程停止后执行
                lock (this)
                {
                    // if (TileMap.Height == 0 || TileMap.Width == 0)
                    // {
                    //     ASeek.manualResetEvent.WaitOne();
                    // }
                    this.DoSeek(targetMap);
                }
            });
        }

        /// <summary>
        /// 根据路径移动
        /// </summary>
        /// <returns>是否到达目标</returns>
        public bool MoveByPath()
        {
            if (this.path.Count == 0)
            {
                return true;
            }

            // 变为真实坐标
            Vector3 worldPos = TileMap.Instance.MapPosToWorldPos(this.path[0].PosMap);

            // 到达路径中一个目标点，切换下一个目标点
            if (this.path.Count != 0 &&
                Mathf.Abs(worldPos.x - this.Character.transform.position.x) < 0.2f &&
                Mathf.Abs(worldPos.y - this.Character.transform.position.y) < 0.2f)
            {
                this.path.RemoveAt(0); // --path.Count
            }

            Vector2 forward = new (worldPos.x - this.Character.transform.position.x, worldPos.y - this.Character.transform.position.y);
            this.Character.transform.Translate(this.Character.MoveSpeed * Time.deltaTime * forward.normalized, Space.World); // 向前移动
            this.UpdateLine(true);
            return false;
        }

        protected virtual void DoSeek(Vector3Int targetMap)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// 更新路径UI
        /// </summary>
        /// <param name="isFirst">是否仅更新第一段线</param>
        protected void UpdateLine(bool isFirst = false)
        {
            if (this.LineRenderer == null)
            {
                return;
            }

            this.LineRenderer.positionCount = this.path.Count + 1;
            this.LineRenderer.SetPosition(this.path.Count, this.Character.transform.position);

            if (isFirst)
            {
                return;
            }

            for (int i = 0; i < this.path.Count; i++)
            {
                this.LineRenderer.SetPosition(this.path.Count - i - 1, TileMap.Instance.MapPosToWorldPos(this.path[i].PosMap));
            }
        }
    }
}
