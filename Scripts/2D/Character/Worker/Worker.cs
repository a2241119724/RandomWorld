using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    [Serializable]
    public class Worker : Character
    {
        [HideInInspector] public WorkerStateManager<ICharacterState, WorkerStateType,Worker> Manager { get; private set; }
        /// <summary>
        /// 是否在寻路
        /// </summary>
        [HideInInspector] public bool IsSeeking { get; set; }
        public Text WorkerState { set; get; }
        public Vector3Int TargetMap { get; set; }
        public float SeekProgress { get; set; }
        //private static int[,] isBlockTiles; // 是否不能通行

        [SerializeField] private float moveSpeed = 2.5f; // 角色速度
        // 数组下标[y,x]与世界坐标(x,y)相反
        private static int cols = 0;
        private static int rows = 0;
        private static Spend[,] mapSpend; // 地图中板块的花费
        // 1为不能通行
        private List<Spend> openList;
        private List<Spend> closeList;
        private List<Spend> path; // 寻路路径
        private Coroutine coroutine; // 寻路路径
        private CheckBug checkBug;
        private readonly List<Vector2SByte> neighbors = new List<Vector2SByte>(){
            new Vector2SByte(0,1), // 上
            new Vector2SByte(1,0), // 右
            new Vector2SByte(0,-1), // 下
            new Vector2SByte(-1,0), // 左
            //new Vector2SByte(1,1), // 右上
            //new Vector2SByte(1,-1), // 右下
            //new Vector2SByte(-1,-1), // 左下
            //new Vector2SByte(-1,1), // 左上
        }; // A*使用哪种邻居

        protected override void Awake()
        {
            base.Awake();
            checkBug = new CheckBug();
            openList = new List<Spend>();
            closeList = new List<Spend>();
            path = new List<Spend>();
            Manager = new WorkerStateManager<ICharacterState, WorkerStateType, Worker>(this);
            name = "Worker";
            MaxHp = Hp = 100;
            WorkerState = transform.Find("State").GetComponent<Text>();
        }

        /// <summary>
        /// 在加入所有状态之后再加到TaskManager中
        /// </summary>
        protected override void Start()
        {
            base.Start();
        }

        void Update()
        {
            // 鼠标点击寻路
            //if (Input.GetMouseButtonUp(0))
            //{
            //    Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            //    getPath(worldPos.x, worldPos.y);
            //}
            transform.rotation = Quaternion.Euler(0, 0, 0);
            // 执行当前状态的函数
            Manager.CurrentState.OnUpdate();
        }

        /// <summary>
        /// 设置地图信息
        /// </summary>
        public static void setMap() {
            rows = TileMap.Instance.Height;
            cols = TileMap.Instance.Width;
            //isBlockTiles = new int[rows, cols];
            //Tiles[,] _mapTiles = TileMap.Instance.MapTiles;
            //for (int i = 0; i < rows; i++)
            //{
            //    for (int j = 0; j < cols; j++)
            //    {
            //        if (_mapTiles[i,j] == Tiles.Mountain)
            //        {
            //            isBlockTiles[i, j] = 1;
            //        }
            //    }
            //}
            // 初始化寻路花费
            mapSpend = new Spend[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    mapSpend[i, j] = new Spend(i, j);
                }
            }
        }

        public void initSeek(Vector3Int targetMap)
        {
            // 停止正在进行的寻路
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
            TargetMap = targetMap;
            IsSeeking = true;
            openList.Clear();
            closeList.Clear();
            path.Clear();
            SeekProgress = 0.0f;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    mapSpend[i, j].init();
                }
            }
        }

        /// <summary>
        /// 开启协程
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void toTarget() {
            //coroutine = StartCoroutine(toTargetLAB(TargetMap));
            // A*
            int startX = Mathf.RoundToInt(transform.position.x);
            int startY = Mathf.RoundToInt(transform.position.y);
            Spend start = mapSpend[startY, startX]; // 起点
            Spend end = mapSpend[TargetMap.x, TargetMap.y]; // 终点
            coroutine = StartCoroutine(toTargetAStar(start,end));
        }

        /// <summary>
        /// 建造不可行
        /// </summary>
        /// <param name="targetMap"></param>
        /// <returns></returns>
        public IEnumerator toTargetLAB(Vector3Int targetMap) {
            if (!TileMap.Instance.isAvailableTile(targetMap))
            {
                Debug.Log("超出边界!!!");
                IsSeeking = false;
                yield break;
            }
            int startX = Mathf.RoundToInt(transform.position.x);
            int startY = Mathf.RoundToInt(transform.position.y);
            Spend start = mapSpend[startY, startX]; // 起点
            Spend end = mapSpend[targetMap.x, targetMap.y]; // 终点
            while (true) {
                Spend mid = straightMove(start, end);
                path.Add(mid);
                // 到达终点
                if (mid.posMap.x == end.posMap.x && mid.posMap.y == end.posMap.y) {
                    break;
                }
                start = findNext(mid, end);
                yield return StartCoroutine(toTargetAStar(mid, start));
            }
            IsSeeking = false;
        }

        /// <summary>
        /// 朝着目标直线走
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns>最后碰到障碍物后走到的位置</returns>
        private Spend straightMove(Spend start, Spend end) {
            float totalDistance = Mathf.Sqrt(Mathf.Pow(start.posMap.x - end.posMap.x, 2) + Mathf.Pow(start.posMap.y - end.posMap.y, 2));
            int detX = end.posMap.x - start.posMap.x;
            int detY = end.posMap.y - start.posMap.y;
            do
            {
                start = mapSpend[end.posMap.x - detX, end.posMap.y - detY];
                SeekProgress = Mathf.Sqrt(Mathf.Pow(start.posMap.x - end.posMap.x, 2) + Mathf.Pow(start.posMap.y - end.posMap.y, 2)) / totalDistance;
                // 到达目标
                if (detX == 0 && detY == 0)
                {
                    return end;
                }
                int max = Mathf.Abs(detX) > Mathf.Abs(detY) ? Mathf.Abs(detX) : Mathf.Abs(detY);
                detX -= Mathf.RoundToInt(detX * 1.0f / max);
                detY -= Mathf.RoundToInt(detY * 1.0f / max);
            } while (isCanReach(new Vector3Int(end.posMap.x - detX, end.posMap.y - detY, 0)));
            return start;
        }

        /// <summary>
        /// 遇到障碍物之后，获取障碍物对面最近的可用位置
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns>障碍物对面最近的可用位置</returns>
        private Spend findNext(Spend start, Spend end)
        {
            int detX = end.posMap.x - start.posMap.x;
            int detY = end.posMap.y - start.posMap.y;
            do
            {
                // 到达目标
                if (detX == 0 && detY == 0)
                {
                    return end;
                }
                int max = Mathf.Abs(detX) > Mathf.Abs(detY) ? Mathf.Abs(detX) : Mathf.Abs(detY);
                detX -= Mathf.RoundToInt(detX * 1.0f / max);
                detY -= Mathf.RoundToInt(detY * 1.0f / max);
            } while (!isCanReach(new Vector3Int(end.posMap.x - detX, end.posMap.y - detY, 0)));
            return mapSpend[end.posMap.x - detX, end.posMap.y - detY];
        }

        /// <summary>
        /// A*算法寻路
        /// </summary>
        private IEnumerator toTargetAStar(Spend start, Spend end)
        {
            int curIterCount = path.Count;
            float totalDistance = Mathf.Sqrt(Mathf.Pow(start.posMap.x - end.posMap.x, 2) + Mathf.Pow(start.posMap.y - end.posMap.y, 2));
            openList.Add(start);
            int count = 0;
            while (openList.Count != 0)
            {
                int minIndex = 0;
                // 选出当前相邻位置最小花费f在openList中的索引位置
                for (int i = 1; i < openList.Count; i++)
                {
                    if (openList[i].f < openList[minIndex].f)
                    {
                        minIndex = i;
                    }
                }
                //if (openList.Count == 0){
                //    break; // 解决bug
                //}
                Spend curSpend = openList[minIndex];
                SeekProgress = Mathf.Sqrt(Mathf.Pow(curSpend.posMap.x - start.posMap.x, 2) + Mathf.Pow(curSpend.posMap.y - start.posMap.y, 2)) / totalDistance;
                // 判断是否到达终点(此处只能是整数)
                if ((int)curSpend.posMap.x == (int)end.posMap.x && (int)curSpend.posMap.y == (int)end.posMap.y)
                {
                    //Debug.Log("找到路径!!!");
                    // 找路径
                    int _count = 0;
                    Vector3Int lastDet = new Vector3Int(0,0);
                    while (curSpend.previous != null)
                    {
                        // 优化(一条直线只存终止节点)
                        if (curSpend.previous.posMap.x - curSpend.posMap.x != lastDet.x || curSpend.previous.posMap.y - curSpend.posMap.y != lastDet.y)
                        {
                            //Debug.Log("经过" + curSpend.pos.x + " " + curSpend.pos.y);
                            path.Insert(curIterCount, curSpend);
                            lastDet.x = curSpend.previous.posMap.x - curSpend.posMap.x;
                            lastDet.y = curSpend.previous.posMap.y - curSpend.posMap.y;
                        }
                        // 可能出现循环路径
                        if (start.posMap.x == curSpend.previous.posMap.x && start.posMap.y == curSpend.previous.posMap.y) {
                            break;
                        }
                        if (++_count > 1000)
                        {
                            Debug.Log("路径长度超过1000!!!");
                        }
                        curSpend = curSpend.previous;
                    }
                    break;
                }
                openList.Remove(curSpend);
                closeList.Add(curSpend);
                // 对邻居进行f = g + h
                byte isCorner = 0;
                foreach (Vector2SByte direction in neighbors) {
                    ++isCorner;
                    int _x = curSpend.posMap.x + direction.x;
                    int _y = curSpend.posMap.y + direction.y;
                    // 数组下标
                    if (!isCanReach(new Vector3Int(_x,_y,0))) continue;
                    Spend neighbor = mapSpend[_x, _y];
                    // 关闭队列不计算
                    if (closeList.Contains(neighbor)) continue;
                    float temp;
                    if (isCorner > 4)
                    {
                        temp = curSpend.g + 1.414f; // 斜着相邻
                    }
                    else
                    {
                        temp = curSpend.g + 1.0f; // 挨着相邻
                    }
                    // 打开队列已经计算过，赋值最小的g
                    if (openList.Contains(neighbor))
                    {
                        // 回溯,放弃该节点
                        if (temp >= neighbor.g) continue;
                        neighbor.g = temp;
                    }
                    else // 不在任何列表中 
                    {
                        neighbor.g = temp;
                        openList.Add(neighbor);
                    }
                    neighbor.h = Mathf.Abs(end.posMap.x - neighbor.posMap.x) + Mathf.Abs(end.posMap.y - neighbor.posMap.y);
                    neighbor.f = neighbor.g + neighbor.h;
                    neighbor.previous = curSpend; // 链接
                }
                if(count++ > 10)
                {
                    count = 0;
                    yield return null;
                }
            }
            if (path.Count == curIterCount)
            {
                Debug.Log("未找到路径:" + start.posMap.y + ":" + start.posMap.x + "-->" + end.posMap.y + ":" + end.posMap.x);
            }
            // ToTargetLAB要注释
            IsSeeking = false;
        }

        /// <summary>
        /// 根据路径移动
        /// </summary>
        public bool moveByPath()
        {
            // 到达路径中一个目标点，切换下一个目标点
            if (path.Count != 0 &&
                Mathf.Abs(path[0].posMap.y - transform.position.x) < 0.01f &&
                Mathf.Abs(path[0].posMap.x - transform.position.y) < 0.01f) {
                path.RemoveAt(0); // --path.Count 
            }
            // remove过后防止后面越界
            if (path.Count == 0) { 
                return true;
            }
            // 变为真实坐标
            Vector2 forward = new Vector2(path[0].posMap.y - transform.position.x, path[0].posMap.x - transform.position.y);
            transform.Translate(forward.normalized * Time.deltaTime * moveSpeed, Space.World);//向前移动
            return false;
        }

        protected override void death()
        {
        }

        /// <summary>
        /// 是否可以抵达(不包含带有碰撞体的Tile,即使是正在建造中的)
        /// </summary>
        /// <param name="posMap"></param>
        /// <returns></returns>
        public bool isCanReach(Vector3Int posMap)
        {
            if (!TileMap.Instance.isAvailableTile(posMap)) {
                return false;
            }
            if(BuildMap.Instance.BuildTileMap.GetTile(posMap) == null)
            {
                return true;
            }
            return Mathf.Abs(BuildMap.Instance.BuildTileMap.GetColor(posMap).a - 0.49f) < 1e-5 
                || Mathf.Abs(BuildMap.Instance.BuildTileMap.GetColor(posMap).a - 0.99f) < 1e-5;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            checkBug.addColliderCount(DateTime.Now.Ticks);
            if (checkBug.isBug())
            {
                initSeek(TargetMap);
                toTarget();
            }
        }

        class CheckBug
        {
            public long lastTime;
            public int colliderCount;

            public bool isBug() {
                return colliderCount > 10;
            }

            public void addColliderCount(long time) {
                if (time - lastTime<100)
                {
                    colliderCount++;
                }
                else
                {
                    colliderCount = 1;
                }
                lastTime = time;
            }
        }
    }

    /// <summary>
    /// f = g + h
    /// </summary>
    class Spend
    {
        // 数组坐标
        public Vector3Int posMap;
        public float f = 0;
        public float g = 0;
        public float h = 0;
        public Spend previous; // 指向路径的前一个位置

        public Spend(int x, int y)
        {
            posMap.x = x;
            posMap.y = y;
        }

        public void init()
        {
            f = g = h = 0;
            previous = null;
        }
    }

    struct Vector2SByte
    {
        public sbyte x;
        public sbyte y;

        public Vector2SByte(sbyte x, sbyte y)
        {
            this.x = x;
            this.y = y;
        }
    }
}