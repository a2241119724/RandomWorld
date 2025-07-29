namespace LAB2D
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using PimDeWitte.UnityMainThreadDispatcher;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Worker
    /// </summary>
    public class Worker : Character
    {
        /// <summary>
        /// 最大疲劳值
        /// </summary>
        public const float MaxTired = 100.0f;

        /// <summary>
        /// 疲劳值阈值
        /// </summary>
        public const float ThresholdTired = 10.0f;

        /// <summary>
        /// 最大饥饿值
        /// </summary>
        public const float MaxHungry = 100.0f;

        /// <summary>
        /// 饥饿值阈值
        /// </summary>
        public const float ThresholdHungry = 10.0f;

        /// <summary>
        /// 锁，防止多个Worker同时寻路
        /// </summary>
        public static Lock SeekLock = new ();

        /// <summary>
        /// 角色装备
        /// </summary>
        public WearData WearData;

        /// <summary>
        /// Worker的床
        /// </summary>
        public BedItem BedItem;

        private static readonly List<Vector2SByte> Neighbors = new ()
        {
            new Vector2SByte(0, 1), new Vector2SByte(1, 0), new Vector2SByte(0, -1), new Vector2SByte(-1, 0), // 上右下左

            // new Vector2SByte(1, 1), new Vector2SByte(1, -1), // 右上,右下
            // new Vector2SByte(-1, -1), new Vector2SByte(-1, 1), // 左下, 左上
        }; // A*使用哪种邻居

        private readonly Vector3[] checkOffsets = { new Vector3(0, 0), new Vector3(-0.5f, 0), new Vector3(0.5f, 0), new Vector3(0, 0.5f), new Vector3(0, -0.5f) }; // 合并path时检测射线偏移
        private volatile Spend[,] mapSpend; // 地图中板块的花费
        private Dictionary<int, ResourceInfo> resourceInfos; // 携带的资源
        private volatile List<Spend> openList;
        private volatile List<Spend> closeList;
        private volatile List<Spend> path; // 寻路路径
        private Slider progress;
        private Text nameUI;
        private CharacterStatusUI statusBar; // 记录实例化血条
        private volatile bool isStopThread = false; // 控制线程停止

        /// <summary>
        /// 状态管理器
        /// </summary>
        [HideInInspector]
        public WorkerStateManager<ICharacterState, WorkerState.WorkerStateTypeEnum, Worker> Manager { get; private set; }

        /// <summary>
        /// 是否在寻路
        /// </summary>
        [HideInInspector]
        public bool IsSeeking { get; set; }

        /// <summary>
        /// Worker状态
        /// </summary>
        public Text WorkerStateText { get; set; }

        /// <summary>
        /// 目标地图坐标
        /// </summary>
        public Vector3Int TargetMap { get; set; }

        /// <summary>
        /// 寻路进度
        /// </summary>
        public float SeekProgress { get; set; }

        /// <summary>
        /// 是否需要做任务的开关
        /// 是否开启做该任务类型的开关(toogle的顺序与TaskType的顺序相关)
        /// </summary>
        public bool[] TaskToggle { get; set; }

        /// <summary>
        /// 当前疲劳值
        /// </summary>
        public float CurTired { get; set; } = 100.0f;

        /// <summary>
        /// 当前饥饿值
        /// </summary>
        public float CurHungry { get; set; } = 100.0f;

        /// <summary>
        /// 最大持有资源数量
        /// </summary>
        public int MaxResourceCount { get; set; } = 30;

        /// <summary>
        /// 寻路路径渲染
        /// </summary>
        public LineRenderer LineRenderer { get; set; }

        /// <summary>
        /// 设置地图信息
        /// </summary>
        /// <param name="height">地图高度</param>
        /// <param name="width">地图宽度</param>
        public void InitMap(int height, int width)
        {
            // 初始化寻路花费
            this.mapSpend = new Spend[height, width];
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    this.mapSpend[i, j] = new Spend(i, j);
                }
            }
        }

        /// <summary>
        /// 初始化寻路信息
        /// </summary>
        /// <param name="targetMap">寻路目标</param>
        public void InitSeek(Vector3Int targetMap)
        {
            if (this.mapSpend == null)
            {
                this.InitMap(TileMap.Height, TileMap.Width);
            }

            // 停止线程
            this.isStopThread = true;
            this.TargetMap = targetMap;
            this.IsSeeking = true;
            this.openList.Clear();
            this.closeList.Clear();
            this.path.Clear();
            this.UpdateLine();
            this.SeekProgress = 0.0f;
            for (int i = 0; i < TileMap.Height; i++)
            {
                for (int j = 0; j < TileMap.Width; j++)
                {
                    this.mapSpend[i, j].Init();
                }
            }
        }

        /// <summary>
        /// 开启协程
        /// </summary>
        public void ToTarget()
        {
            // A*
            Vector3Int posMap = TileMap.Instance.WorldPosToMapPos(this.transform.position);
            Spend start = this.mapSpend[posMap.x, posMap.y]; // 起点
            Spend end = this.mapSpend[this.TargetMap.x, this.TargetMap.y]; // 终点

            ThreadPool.QueueUserWorkItem(t =>
            {
                this.ToTargetAStarThread(start, end);
            });
        }

        /// <summary>
        /// 建造不可行
        /// </summary>
        /// <param name="targetMap">目标坐标</param>
        /// <returns>迭代器</returns>
        public IEnumerator ToTargetLAB(Vector3Int targetMap)
        {
            if (!TileMap.Instance.IsFreeTile(targetMap))
            {
                LogManager.Instance.Log("超出边界!!!", LogManager.LogLevel.Error);
                this.IsSeeking = false;
                yield break;
            }

            Vector3Int posMap = TileMap.Instance.WorldPosToMapPos(this.transform.position);
            Spend start = this.mapSpend[posMap.x, posMap.y]; // 起点
            Spend end = this.mapSpend[targetMap.x, targetMap.y]; // 终点
            while (true)
            {
                Spend mid = this.StraightMove(start, end);
                this.path.Add(mid);

                // 到达终点
                if (mid.PosMap.x == end.PosMap.x && mid.PosMap.y == end.PosMap.y)
                {
                    break;
                }

                start = this.FindNext(mid, end);
                yield return this.StartCoroutine(this.ToTargetAStar(mid, start));
            }

            this.IsSeeking = false;
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
                Mathf.Abs(worldPos.x - this.transform.position.x) < 0.2f &&
                Mathf.Abs(worldPos.y - this.transform.position.y) < 0.2f)
            {
                this.path.RemoveAt(0); // --path.Count
            }

            Vector2 forward = new (worldPos.x - this.transform.position.x, worldPos.y - this.transform.position.y);
            this.transform.Translate(this.MoveSpeed * Time.deltaTime * forward.normalized, Space.World); // 向前移动
            this.UpdateLine();
            return false;
        }

        /// <summary>
        /// 是否可以抵达(不包含带有碰撞体的Tile,即使是正在建造中的)
        /// </summary>
        /// <param name="posMap">目标坐标</param>
        /// <returns>是否</returns>
        public bool IsCanReach(Vector3Int posMap)
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

        /// <summary>
        /// 设置任务进度条
        /// </summary>
        /// <param name="value">进度值</param>
        /// <param name="enable">是否显示进度条</param>
        public void SetProgress(float value, bool enable)
        {
            this.progress.value = value;
            this.progress.gameObject.SetActive(enable);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string resources = string.Empty;
            foreach (KeyValuePair<int, ResourceInfo> resource in this.resourceInfos)
            {
                resources += resource.Key + ":" + resource.Value.Count + "\n";
            }

            return base.ToString() +
                $"Hungry:{this.CurHungry}\n" +
                $"TargetMap:{this.TargetMap}\n" +
                resources;
        }

        /// <summary>
        /// 添加携带的资源
        /// </summary>
        /// <param name="resourceInfo">资源</param>
        public void AddResource(ResourceInfo resourceInfo)
        {
            if (resourceInfo.Count == 0)
            {
                return;
            }

            if (this.resourceInfos.ContainsKey(resourceInfo.Id))
            {
                this.resourceInfos[resourceInfo.Id].Count += resourceInfo.Count;
            }
            else
            {
                this.resourceInfos.Add(resourceInfo.Id, Tool.DeepCopyByBinary(resourceInfo));
            }
        }

        /// <summary>
        /// 删除携带的资源
        /// </summary>
        /// <param name="needResource">资源</param>
        public void SubResource(Dictionary<int, ResourceInfo> needResource)
        {
            foreach (KeyValuePair<int, ResourceInfo> need in needResource)
            {
                if (this.resourceInfos.ContainsKey(need.Key))
                {
                    this.resourceInfos[need.Key].Count -= need.Value.Count;
                }
                else
                {
                    LogManager.Instance.Log("自身资源不够，仍然建造成功，错误", LogManager.LogLevel.Error);
                }
            }
        }

        /// <summary>
        /// 删除携带的资源
        /// </summary>
        /// <param name="resourceInfo">资源</param>
        public void SubResource(ResourceInfo resourceInfo)
        {
            if (resourceInfo.Count == 0)
            {
                return;
            }

            if (this.resourceInfos.ContainsKey(resourceInfo.Id))
            {
                this.resourceInfos[resourceInfo.Id].Count -= resourceInfo.Count;
            }
            else
            {
                LogManager.Instance.Log("自身资源不够，仍然建造成功，错误", LogManager.LogLevel.Error);
            }
        }

        /// <summary>
        /// 根据ID获得携带的资源数量
        /// </summary>
        /// <param name="id">资源ID</param>
        /// <returns>资源数量</returns>
        public int GetResourceCountById(int id)
        {
            if (this.resourceInfos.ContainsKey(id))
            {
                return this.resourceInfos[id].Count;
            }

            return 0;
        }

        /// <summary>
        /// 判断worker携带的资源够不够建造
        /// </summary>
        /// <param name="needResource">需要建造的资源</param>
        /// <returns>是否</returns>
        public bool IsEnough(Dictionary<int, ResourceInfo> needResource)
        {
            foreach (KeyValuePair<int, ResourceInfo> need in needResource)
            {
                if (!this.resourceInfos.ContainsKey(need.Key) || this.resourceInfos[need.Key].Count < need.Value.Count)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 放弃任务
        /// </summary>
        public void GiveUpTask()
        {
            WorkerTaskManager.Instance.GiveUpTask(this.Manager.Task);
            this.Manager.Task = null;
            this.Manager.ChangeState(WorkerState.WorkerStateTypeEnum.Seek);
        }

        /// <summary>
        /// 建造所需要的资源数量减去worker身上所带的资源数量
        /// </summary>
        /// <param name="needResource">需要的资源</param>
        /// <returns>Worker携带不够的资源数</returns>
        public Dictionary<int, ResourceInfo> GetRemaining(Dictionary<int, ResourceInfo> needResource)
        {
            Dictionary<int, ResourceInfo> remaining = new ();
            foreach (KeyValuePair<int, ResourceInfo> need in needResource)
            {
                if (this.resourceInfos.ContainsKey(need.Key))
                {
                    remaining.Add(need.Key, new ResourceInfo(need.Key, need.Value.Count - this.resourceInfos[need.Key].Count));
                }
                else
                {
                    remaining.Add(need.Key, Tool.DeepCopyByBinary(need.Value));
                }
            }

            return remaining;
        }

        /// <summary>
        /// 掉血
        /// </summary>
        /// <param name="hp">所掉的血量</param>
        public override void ReduceHp(float hp)
        {
            if (hp <= 0)
            {
                LogManager.Instance.Log("Hp can't less than zero!!!", LogManager.LogLevel.Error);
                return;
            }

            base.ReduceHp(hp);
            this.statusBar.UpdateStatus(this.CharacterDataLAB.Hp, this.CharacterDataLAB.MaxHp);
            this.Manager.ChangeState(WorkerState.WorkerStateTypeEnum.Attack);
        }

        /// <inheritdoc/>
        protected override void Death()
        {
        }

        /// <inheritdoc/>
        protected override void Awake()
        {
            base.Awake();
            this.openList = new List<Spend>();
            this.closeList = new List<Spend>();
            this.path = new List<Spend>();
            this.Manager = new WorkerStateManager<ICharacterState, WorkerState.WorkerStateTypeEnum, Worker>(this);
            this.CharacterDataLAB.MaxHp = this.CharacterDataLAB.Hp = 100;
            this.nameUI = this.transform.Find("Name").GetComponent<Text>();
            this.WorkerStateText = this.transform.Find("State").GetComponent<Text>();
            this.progress = this.transform.Find("Progress").GetComponent<Slider>();
            this.progress.gameObject.SetActive(false);

            // 路径
            this.LineRenderer = this.transform.GetComponent<LineRenderer>();
            this.LineRenderer.startWidth = 0.05f;
            this.LineRenderer.endWidth = 0.05f;
            Material material = new (Shader.Find("Unlit/Color"));
            material.color = new Color(UnityEngine.Random.Range(0.5f, 1.0f), UnityEngine.Random.Range(0.5f, 1.0f), UnityEngine.Random.Range(0.5f, 1.0f));
            this.LineRenderer.material = material;
            this.LineRenderer.sortingLayerName = "Highest";

            this.TaskToggle = new bool[10];

            // 默认可以吃饭
            this.TaskToggle[(int)WorkerTask.WorkerTaskTypeEnum.Eat] = true;
            this.TaskToggle[(int)WorkerTask.WorkerTaskTypeEnum.Wear] = true;
            this.TaskToggle[(int)WorkerTask.WorkerTaskTypeEnum.Carry] = true;
            this.resourceInfos = new Dictionary<int, ResourceInfo>();
            this.statusBar = this.transform.Find("Hp").GetComponent<CharacterStatusUI>();
            if (this.statusBar == null)
            {
                LogManager.Instance.Log("statusBar Not Found!!!", LogManager.LogLevel.Error);
                return;
            }

            this.WearData = new WearData();
            ThreadPool.SetMaxThreads(5, 5);
        }

        /// <summary>
        /// 在加入所有状态之后再加到TaskManager中
        /// </summary>
        protected override void Start()
        {
            base.Start();
            this.nameUI.text = this.name;
            this.statusBar.UpdateStatus(this.CharacterDataLAB.Hp, this.CharacterDataLAB.MaxHp);
        }

        private void Update()
        {
            this.transform.rotation = Quaternion.Euler(0, 0, 0);

            // 执行当前状态的函数
            this.Manager.CurrentState.OnUpdate();
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            this.checkBug.AddColliderCount(DateTime.Now.Ticks);
            if (this.checkBug.IsBug(this.name, 100))
            {
                this.Manager.ChangeState(WorkerState.WorkerStateTypeEnum.Seek);
            }
        }

        /// <summary>
        /// 更新路径UI
        /// </summary>
        private void UpdateLine()
        {
            this.LineRenderer.positionCount = this.path.Count + 1;
            this.LineRenderer.SetPosition(0, this.transform.position);
            for (int i = 0; i < this.path.Count; i++)
            {
                this.LineRenderer.SetPosition(i + 1, TileMap.Instance.MapPosToWorldPos(this.path[i].PosMap));
            }
        }

        /// <summary>
        /// 朝着目标直线走
        /// </summary>
        /// <param name="start">起始位置</param>
        /// <param name="end">终点位置</param>
        /// <returns>最后碰到障碍物后走到的位置</returns>
        private Spend StraightMove(Spend start, Spend end)
        {
            float totalDistance = Mathf.Sqrt(Mathf.Pow(start.PosMap.x - end.PosMap.x, 2) + Mathf.Pow(start.PosMap.y - end.PosMap.y, 2));
            int detX = end.PosMap.x - start.PosMap.x;
            int detY = end.PosMap.y - start.PosMap.y;
            do
            {
                start = this.mapSpend[end.PosMap.x - detX, end.PosMap.y - detY];
                this.SeekProgress = Mathf.Sqrt(Mathf.Pow(start.PosMap.x - end.PosMap.x, 2) + Mathf.Pow(start.PosMap.y - end.PosMap.y, 2)) / totalDistance;

                // 到达目标
                if (detX == 0 && detY == 0)
                {
                    return end;
                }

                int max = Mathf.Abs(detX) > Mathf.Abs(detY) ? Mathf.Abs(detX) : Mathf.Abs(detY);
                detX -= Mathf.RoundToInt(detX * 1.0f / max);
                detY -= Mathf.RoundToInt(detY * 1.0f / max);
            }
            while (this.IsCanReach(new Vector3Int(end.PosMap.x - detX, end.PosMap.y - detY, 0)));
            return start;
        }

        /// <summary>
        /// 遇到障碍物之后，获取障碍物对面最近的可用位置
        /// </summary>
        /// <param name="start">起始位置</param>
        /// <param name="end">终点位置</param>
        /// <returns>障碍物对面最近的可用位置</returns>
        private Spend FindNext(Spend start, Spend end)
        {
            int detX = end.PosMap.x - start.PosMap.x;
            int detY = end.PosMap.y - start.PosMap.y;
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
            }
            while (!this.IsCanReach(new Vector3Int(end.PosMap.x - detX, end.PosMap.y - detY, 0)));
            return this.mapSpend[end.PosMap.x - detX, end.PosMap.y - detY];
        }

        /// <summary>
        /// A*算法寻路
        /// </summary>
        private IEnumerator ToTargetAStar(Spend start, Spend end)
        {
            // 超过一定时间释放锁
            float time = 0.0f;

            // 记录一开始的path长度
            int curIterCount = this.path.Count;
            List<Spend> path = new ();
            float totalDistance = Mathf.Sqrt(Mathf.Pow(start.PosMap.x - end.PosMap.x, 2)
                + Mathf.Pow(start.PosMap.y - end.PosMap.y, 2));
            this.openList.Add(start);
            while (this.openList.Count != 0)
            {
                int minIndex = 0;

                // 选出当前相邻位置最小花费f在openList中的索引位置
                for (int i = 1; i < this.openList.Count; i++)
                {
                    if (this.openList[i].F < this.openList[minIndex].F)
                    {
                        minIndex = i;
                    }
                }

                // if (openList.Count == 0){
                //     break; // 解决bug
                // }
                Spend curSpend = this.openList[minIndex];
                this.SeekProgress = Mathf.Sqrt(Mathf.Pow(curSpend.PosMap.x - start.PosMap.x, 2)
                    + Mathf.Pow(curSpend.PosMap.y - start.PosMap.y, 2)) / totalDistance;

                // 判断是否到达终点(此处只能是整数)
                if ((int)curSpend.PosMap.x == (int)end.PosMap.x && (int)curSpend.PosMap.y == (int)end.PosMap.y)
                {
                    // LogManager.Instance.log("找到路径!!!", LogManager.LogLevel.Info);
                    // 找路径
                    Vector3Int lastDet = new (0, 0);
                    Spend curSpend1 = curSpend;
                    while (curSpend != null && curSpend.Previous != null)
                    {
                        // // 优化(一条直线只存终止节点)
                        // if (curSpend.previous.posMap.x - curSpend.posMap.x != lastDet.x || curSpend.previous.posMap.y - curSpend.posMap.y != lastDet.y)
                        // {
                        //     //LogManager.Instance.log("经过" + curSpend.posMap.y + " " + curSpend.posMap.x, LogManager.LogLevel.Info);
                        //     _path.Insert(curIterCount, curSpend);
                        //     lastDet.x = curSpend.previous.posMap.x - curSpend.posMap.x;
                        //     lastDet.y = curSpend.previous.posMap.y - curSpend.posMap.y;
                        // }
                        path.Insert(curIterCount, curSpend);

                        // 可能出现循环路径
                        if (curSpend1 != null)
                        {
                            curSpend1 = curSpend1.Previous;
                            if (curSpend1 != null)
                            {
                                curSpend1 = curSpend1.Previous;
                            }
                        }

                        if (curSpend1 != null && curSpend1.PosMap.x == curSpend.Previous.PosMap.x
                            && curSpend1.PosMap.y == curSpend.Previous.PosMap.y)
                        {
                            LogManager.Instance.Log("Worker寻路出现环路", LogManager.LogLevel.Error);
                            break;
                        }

                        if (FrameControl.Instance.IsNeedStop())
                        {
                            yield return null;
                        }

                        curSpend = curSpend.Previous;
                    }

                    break;
                }

                this.openList.Remove(curSpend);
                this.closeList.Add(curSpend);

                // 对邻居进行f = g + h
                byte isCorner = 0;
                foreach (Vector2SByte direction in Neighbors)
                {
                    ++isCorner;
                    int x = curSpend.PosMap.x + direction.X;
                    int y = curSpend.PosMap.y + direction.Y;

                    // 数组下标
                    if (!this.IsCanReach(new Vector3Int(x, y, 0)))
                    {
                        continue;
                    }

                    Spend neighbor = this.mapSpend[x, y];

                    // 关闭队列不计算
                    if (this.closeList.Contains(neighbor))
                    {
                        continue;
                    }

                    float temp;
                    if (isCorner > 4)
                    {
                        // 当上下左右阻塞时，斜着不可走
                        if (!this.IsCanReach(new Vector3Int(x, curSpend.PosMap.y, 0))
                            && !this.IsCanReach(new Vector3Int(curSpend.PosMap.x, y, 0)))
                        {
                            continue;
                        }

                        temp = curSpend.G + 1.414f; // 斜着相邻
                    }
                    else
                    {
                        temp = curSpend.G + 1.0f; // 挨着相邻
                    }

                    // 打开队列已经计算过，赋值最小的g
                    if (this.openList.Contains(neighbor))
                    {
                        // 回溯,放弃该节点
                        if (temp >= neighbor.G)
                        {
                            continue;
                        }

                        neighbor.G = temp;
                    }

                    // 不在任何列表中
                    else
                    {
                        neighbor.G = temp;
                        this.openList.Add(neighbor);
                    }

                    neighbor.H = Mathf.Abs(end.PosMap.x - neighbor.PosMap.x) + Mathf.Abs(end.PosMap.y - neighbor.PosMap.y);
                    neighbor.F = neighbor.G + neighbor.H;
                    neighbor.Previous = curSpend; // 链接
                }

                if (FrameControl.Instance.IsNeedStop())
                {
                    time += Time.deltaTime;
                    if (time > 1.0f)
                    {
                        // 如果寻路超过一定时间释放锁
                        SeekLock.ReleaseLock(this);
                        time = 0.0f;
                    }

                    yield return null;

                    // 被其他人持有锁，等待
                    yield return new WaitUntil(() => SeekLock.GetLock(this));
                }
            }

            // 优化，射线检测
            // if (_path.Count > 1)
            // {
            //     start = _path[0];
            //     path.Add(start);
            //     for (int i = 2; i < _path.Count; i++)
            //     {
            //         RaycastHit2D hit = Physics2D.Raycast(TileMap.Instance.mapPosToWorldPos(start.posMap),
            //             TileMap.Instance.mapPosToWorldPos(_path[i].posMap) - TileMap.Instance.mapPosToWorldPos(start.posMap),
            //             Vector3.Distance(TileMap.Instance.mapPosToWorldPos(start.posMap), TileMap.Instance.mapPosToWorldPos(_path[i].posMap)));
            //         if (hit.collider == null) continue;
            //         // 当前节点不可直接直线走，加入上一个节点
            //         path.Add(_path[i - 1]);
            //         start = _path[i - 1];
            //     }
            //     path.Add(_path[_path.Count - 1]);
            // }
            // 合并path
            if (path.Count > 1)
            {
                int lastIndex = 0;
                while (lastIndex < path.Count - 1)
                {
                    int count = 0;
                    bool isUpdate = false;
                    start = path[lastIndex];
                    this.path.Add(start);
                    for (int i = lastIndex + 1; i < path.Count; i++)
                    {
                        if (count > 50)
                        {
                            break;
                        }

                        if (FrameControl.Instance.IsNeedStop())
                        {
                            yield return null;
                        }

                        // 上下平移一下射线
                        Vector3 pos = TileMap.Instance.MapPosToWorldPos(start.PosMap);
                        Vector3 direction = TileMap.Instance.MapPosToWorldPos(path[i].PosMap) - TileMap.Instance.MapPosToWorldPos(start.PosMap);
                        float distance = Vector3.Distance(TileMap.Instance.MapPosToWorldPos(start.PosMap), TileMap.Instance.MapPosToWorldPos(path[i].PosMap));
                        RaycastHit2D hit = Physics2D.Raycast(new Vector2(pos.x - 0.5f, pos.y), direction, distance);
                        if (hit.collider == null)
                        {
                            hit = Physics2D.Raycast(new Vector2(pos.x + 0.5f, pos.y), direction, distance);
                        }

                        if (hit.collider == null)
                        {
                            lastIndex = i;
                            isUpdate = true;
                        }
                    }

                    if (!isUpdate)
                    {
                        lastIndex++;
                    }
                }

                this.path.Add(path[^1]);
            }

            if (this.path.Count == curIterCount)
            {
                LogManager.Instance.Log(this.name + "未找到路径:" + start.PosMap.y + ":" + start.PosMap.x + "-->" + end.PosMap.y + ":" + end.PosMap.x, LogManager.LogLevel.Error);
            }

            // 显示路径
            this.UpdateLine();

            // ToTargetLAB要注释
            this.IsSeeking = false;

            // seekLock.releaseLock(this);
        }

        /// <summary>
        /// A*算法寻路
        /// </summary>
        private void ToTargetAStarThread(Spend start, Spend end)
        {
            this.isStopThread = false;
            List<Spend> path = new ();
            float totalDistance = Mathf.Sqrt(Mathf.Pow(start.PosMap.x - end.PosMap.x, 2)
                + Mathf.Pow(start.PosMap.y - end.PosMap.y, 2));
            this.openList.Add(start);
            while (!this.isStopThread && this.openList.Count != 0)
            {
                int minIndex = 0;

                if (this.isStopThread)
                {
                    break;
                }

                // 选出当前相邻位置最小花费f在openList中的索引位置
                for (int i = 1; i < this.openList.Count; i++)
                {
                    if (this.isStopThread)
                    {
                        break;
                    }

                    if (this.openList[i].F < this.openList[minIndex].F)
                    {
                        minIndex = i;
                    }
                }

                if (this.isStopThread)
                {
                    break;
                }

                Spend curSpend = this.openList[minIndex];
                this.SeekProgress = Mathf.Sqrt(Mathf.Pow(curSpend.PosMap.x - start.PosMap.x, 2)
                    + Mathf.Pow(curSpend.PosMap.y - start.PosMap.y, 2)) / totalDistance;

                // 判断是否到达终点(此处只能是整数)
                if ((int)curSpend.PosMap.x == (int)end.PosMap.x && (int)curSpend.PosMap.y == (int)end.PosMap.y)
                {
                    // LogManager.Instance.log("找到路径!!!", LogManager.LogLevel.Info);
                    // 找路径
                    Vector3Int lastDet = new (0, 0);
                    Spend quickCurSpend = curSpend;
                    while (!this.isStopThread && curSpend != null && curSpend.Previous != null)
                    {
                        path.Insert(0, curSpend);

                        // 可能出现循环路径
                        if (quickCurSpend != null)
                        {
                            quickCurSpend = quickCurSpend.Previous;
                            if (quickCurSpend != null)
                            {
                                quickCurSpend = quickCurSpend.Previous;
                            }
                        }

                        if (quickCurSpend != null && quickCurSpend.PosMap.x == curSpend.Previous.PosMap.x
                            && quickCurSpend.PosMap.y == curSpend.Previous.PosMap.y)
                        {
                            UnityMainThreadDispatcher.Instance().EnqueueAsync(() =>
                            {
                                LogManager.Instance.Log(this.name + ":寻路出现环路", LogManager.LogLevel.Error);
                            }).Wait();
                            break;
                        }

                        curSpend = curSpend.Previous;
                    }

                    break;
                }

                if (this.isStopThread)
                {
                    break;
                }

                this.openList.Remove(curSpend);
                this.closeList.Add(curSpend);

                // 对邻居进行f = g + h
                byte isCorner = 0;
                foreach (Vector2SByte direction in Neighbors)
                {
                    ++isCorner;
                    int x = curSpend.PosMap.x + direction.X;
                    int y = curSpend.PosMap.y + direction.Y;

                    bool isReach = true;
                    UnityMainThreadDispatcher.Instance().EnqueueAsync(() =>
                    {
                        isReach = this.IsCanReach(new Vector3Int(x, y, 0));
                    }).Wait();

                    // 数组下标
                    if (!isReach)
                    {
                        continue;
                    }

                    Spend neighbor = this.mapSpend[x, y];

                    // 关闭队列不计算
                    if (this.closeList.Contains(neighbor))
                    {
                        continue;
                    }

                    float temp;
                    if (isCorner > 4)
                    {
                        UnityMainThreadDispatcher.Instance().EnqueueAsync(() =>
                        {
                            isReach = this.IsCanReach(new Vector3Int(x, curSpend.PosMap.y, 0)) || this.IsCanReach(new Vector3Int(curSpend.PosMap.x, y, 0));
                        }).Wait();

                        // 当上下左右阻塞时，斜着不可走
                        if (!isReach)
                        {
                            continue;
                        }

                        temp = curSpend.G + 1.414f; // 斜着相邻
                    }
                    else
                    {
                        temp = curSpend.G + 1.0f; // 挨着相邻
                    }

                    if (this.isStopThread)
                    {
                        break;
                    }

                    // 打开队列已经计算过，赋值最小的g
                    if (this.openList.Contains(neighbor))
                    {
                        // 回溯,放弃该节点
                        if (temp >= neighbor.G)
                        {
                            continue;
                        }

                        neighbor.G = temp;
                    }

                    // 不在任何列表中
                    else
                    {
                        neighbor.G = temp;

                        if (this.isStopThread)
                        {
                            break;
                        }

                        this.openList.Add(neighbor);
                    }

                    neighbor.H = Mathf.Abs(end.PosMap.x - neighbor.PosMap.x) + Mathf.Abs(end.PosMap.y - neighbor.PosMap.y);
                    neighbor.F = neighbor.G + neighbor.H;
                    neighbor.Previous = curSpend; // 链接
                }
            }

            // 合并path
            if (path.Count > 1)
            {
                int lastIndex = 0;
                while (!this.isStopThread && lastIndex < path.Count - 1)
                {
                    bool isUpdate = false;
                    start = path[lastIndex];

                    // 不加入起点第一个位置
                    if (lastIndex != 0)
                    {
                        this.path.Add(start);
                    }

                    // 在一定path范围内, 倒叙遍历最后一个直达的位置
                    int scope = Mathf.Min(50, path.Count - lastIndex - 1);
                    for (int i = scope; i >= lastIndex + 1; i--)
                    {
                        if (this.isStopThread)
                        {
                            break;
                        }

                        // 上下左右平移一下射线
                        Vector3 pos = TileMap.Instance.MapPosToWorldPos(start.PosMap);
                        Vector3 direction = TileMap.Instance.MapPosToWorldPos(path[i].PosMap) - TileMap.Instance.MapPosToWorldPos(start.PosMap);
                        float distance = Vector3.Distance(TileMap.Instance.MapPosToWorldPos(start.PosMap), TileMap.Instance.MapPosToWorldPos(path[i].PosMap));

                        bool isAllCanReach = true;
                        UnityMainThreadDispatcher.Instance().EnqueueAsync(() =>
                        {
                            RaycastHit2D hit;
                            foreach (var offset in this.checkOffsets)
                            {
                                hit = Physics2D.Raycast(pos + offset, direction, distance);
                                if (hit.collider != null && hit.collider.name.Contains("Map"))
                                {
                                    isAllCanReach = false;
                                    break;
                                }
                            }
                        }).Wait();

                        if (isAllCanReach)
                        {
                            lastIndex = i;
                            isUpdate = true;
                            break;
                        }
                    }

                    if (!isUpdate)
                    {
                        lastIndex++;
                    }
                }

                this.path.Add(path[^1]);
            }
            else
            {
                UnityMainThreadDispatcher.Instance().EnqueueAsync(() =>
                {
                    LogManager.Instance.Log(this.name + ":未找到路径 " + start.PosMap.y + ":" + start.PosMap.x + "-->" + end.PosMap.y + ":" + end.PosMap.x, LogManager.LogLevel.Error);
                }).Wait();
            }

            // 显示路径
            UnityMainThreadDispatcher.Instance().EnqueueAsync(() =>
            {
                this.UpdateLine();
            }).Wait();

            // ToTargetLAB要注释
            this.IsSeeking = false;

            // seekLock.releaseLock(this);
        }
    }

    /// <summary>
    /// f = g + h
    /// </summary>
    public class Spend
    {
        /// <summary>
        /// 坐标
        /// </summary>
        public Vector3Int PosMap;

        /// <summary>
        /// 预估总消耗
        /// </summary>
        public float F = 0;

        /// <summary>
        /// 已经的消耗
        /// </summary>
        public float G = 0;

        /// <summary>
        /// 后续预估的消耗
        /// </summary>
        public float H = 0;

        /// <summary>
        /// 指向路径的前一个位置
        /// </summary>
        public Spend Previous;

        public Spend(int x, int y)
        {
            this.PosMap.x = x;
            this.PosMap.y = y;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public void Init()
        {
            this.F = this.G = this.H = 0;
            this.Previous = null;
        }
    }

    /// <summary>
    /// Byte的Vector2
    /// </summary>
    public class Vector2SByte
    {
        /// <summary>
        /// X 坐标
        /// </summary>
        public sbyte X;

        /// <summary>
        /// Y 坐标
        /// </summary>
        public sbyte Y;

        public Vector2SByte(sbyte x, sbyte y)
        {
            this.X = x;
            this.Y = y;
        }
    }

    /// <summary>
    /// 角色装备数据
    /// </summary>
    public class WearData
    {
        /// <summary>
        /// 携带的武器
        /// </summary>
        public Weapon Weapon;

        /// <summary>
        /// 身上携带的装备
        /// </summary>
        public Dictionary<Equipment.EquipType, Equipment> Equipments;

        public WearData()
        {
            this.Equipments = new Dictionary<Equipment.EquipType, Equipment>();
        }

        /// <summary>
        /// 添加装备
        /// </summary>
        /// <param name="equipment">装备</param>
        /// <param name="posMap">位置</param>
        public void AddEquipment(Equipment equipment, Vector3Int posMap)
        {
            if (this.Equipments.ContainsKey(equipment.EquipTypeValue))
            {
                // 交换装备
                Equipment equipment1 = this.Equipments[equipment.EquipTypeValue];
                ItemMap.Instance.PutDownToInventory(posMap, ResourceManager.Instance.GetAsset(equipment.ToString()), new ResourceInfo(equipment.Id, 1));
                this.Equipments[equipment.EquipTypeValue] = equipment1;
            }
            else
            {
                this.Equipments.Add(equipment.EquipTypeValue, equipment);
            }
        }
    }
}