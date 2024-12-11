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
        /// �Ƿ���Ѱ·
        /// </summary>
        [HideInInspector] public bool IsSeeking { get; set; }
        public Text WorkerState { set; get; }
        public Vector3Int TargetMap { get; set; }
        public float SeekProgress { get; set; }
        //private static int[,] isBlockTiles; // �Ƿ���ͨ��

        [SerializeField] private float moveSpeed = 2.5f; // ��ɫ�ٶ�
        // �����±�[y,x]����������(x,y)�෴
        private static int cols = 0;
        private static int rows = 0;
        private static Spend[,] mapSpend; // ��ͼ�а��Ļ���
        // 1Ϊ����ͨ��
        private List<Spend> openList;
        private List<Spend> closeList;
        private List<Spend> path; // Ѱ··��
        private Coroutine coroutine; // Ѱ··��
        private CheckBug checkBug;
        private readonly List<Vector2SByte> neighbors = new List<Vector2SByte>(){
            new Vector2SByte(0,1), // ��
            new Vector2SByte(1,0), // ��
            new Vector2SByte(0,-1), // ��
            new Vector2SByte(-1,0), // ��
            //new Vector2SByte(1,1), // ����
            //new Vector2SByte(1,-1), // ����
            //new Vector2SByte(-1,-1), // ����
            //new Vector2SByte(-1,1), // ����
        }; // A*ʹ�������ھ�

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
        /// �ڼ�������״̬֮���ټӵ�TaskManager��
        /// </summary>
        protected override void Start()
        {
            base.Start();
        }

        void Update()
        {
            // �����Ѱ·
            //if (Input.GetMouseButtonUp(0))
            //{
            //    Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            //    getPath(worldPos.x, worldPos.y);
            //}
            transform.rotation = Quaternion.Euler(0, 0, 0);
            // ִ�е�ǰ״̬�ĺ���
            Manager.CurrentState.OnUpdate();
        }

        /// <summary>
        /// ���õ�ͼ��Ϣ
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
            // ��ʼ��Ѱ·����
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
            // ֹͣ���ڽ��е�Ѱ·
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
        /// ����Э��
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void toTarget() {
            //coroutine = StartCoroutine(toTargetLAB(TargetMap));
            // A*
            int startX = Mathf.RoundToInt(transform.position.x);
            int startY = Mathf.RoundToInt(transform.position.y);
            Spend start = mapSpend[startY, startX]; // ���
            Spend end = mapSpend[TargetMap.x, TargetMap.y]; // �յ�
            coroutine = StartCoroutine(toTargetAStar(start,end));
        }

        /// <summary>
        /// ���첻����
        /// </summary>
        /// <param name="targetMap"></param>
        /// <returns></returns>
        public IEnumerator toTargetLAB(Vector3Int targetMap) {
            if (!TileMap.Instance.isAvailableTile(targetMap))
            {
                Debug.Log("�����߽�!!!");
                IsSeeking = false;
                yield break;
            }
            int startX = Mathf.RoundToInt(transform.position.x);
            int startY = Mathf.RoundToInt(transform.position.y);
            Spend start = mapSpend[startY, startX]; // ���
            Spend end = mapSpend[targetMap.x, targetMap.y]; // �յ�
            while (true) {
                Spend mid = straightMove(start, end);
                path.Add(mid);
                // �����յ�
                if (mid.posMap.x == end.posMap.x && mid.posMap.y == end.posMap.y) {
                    break;
                }
                start = findNext(mid, end);
                yield return StartCoroutine(toTargetAStar(mid, start));
            }
            IsSeeking = false;
        }

        /// <summary>
        /// ����Ŀ��ֱ����
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns>��������ϰ�����ߵ���λ��</returns>
        private Spend straightMove(Spend start, Spend end) {
            float totalDistance = Mathf.Sqrt(Mathf.Pow(start.posMap.x - end.posMap.x, 2) + Mathf.Pow(start.posMap.y - end.posMap.y, 2));
            int detX = end.posMap.x - start.posMap.x;
            int detY = end.posMap.y - start.posMap.y;
            do
            {
                start = mapSpend[end.posMap.x - detX, end.posMap.y - detY];
                SeekProgress = Mathf.Sqrt(Mathf.Pow(start.posMap.x - end.posMap.x, 2) + Mathf.Pow(start.posMap.y - end.posMap.y, 2)) / totalDistance;
                // ����Ŀ��
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
        /// �����ϰ���֮�󣬻�ȡ�ϰ����������Ŀ���λ��
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns>�ϰ����������Ŀ���λ��</returns>
        private Spend findNext(Spend start, Spend end)
        {
            int detX = end.posMap.x - start.posMap.x;
            int detY = end.posMap.y - start.posMap.y;
            do
            {
                // ����Ŀ��
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
        /// A*�㷨Ѱ·
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
                // ѡ����ǰ����λ����С����f��openList�е�����λ��
                for (int i = 1; i < openList.Count; i++)
                {
                    if (openList[i].f < openList[minIndex].f)
                    {
                        minIndex = i;
                    }
                }
                //if (openList.Count == 0){
                //    break; // ���bug
                //}
                Spend curSpend = openList[minIndex];
                SeekProgress = Mathf.Sqrt(Mathf.Pow(curSpend.posMap.x - start.posMap.x, 2) + Mathf.Pow(curSpend.posMap.y - start.posMap.y, 2)) / totalDistance;
                // �ж��Ƿ񵽴��յ�(�˴�ֻ��������)
                if ((int)curSpend.posMap.x == (int)end.posMap.x && (int)curSpend.posMap.y == (int)end.posMap.y)
                {
                    //Debug.Log("�ҵ�·��!!!");
                    // ��·��
                    int _count = 0;
                    Vector3Int lastDet = new Vector3Int(0,0);
                    while (curSpend.previous != null)
                    {
                        // �Ż�(һ��ֱ��ֻ����ֹ�ڵ�)
                        if (curSpend.previous.posMap.x - curSpend.posMap.x != lastDet.x || curSpend.previous.posMap.y - curSpend.posMap.y != lastDet.y)
                        {
                            //Debug.Log("����" + curSpend.pos.x + " " + curSpend.pos.y);
                            path.Insert(curIterCount, curSpend);
                            lastDet.x = curSpend.previous.posMap.x - curSpend.posMap.x;
                            lastDet.y = curSpend.previous.posMap.y - curSpend.posMap.y;
                        }
                        // ���ܳ���ѭ��·��
                        if (start.posMap.x == curSpend.previous.posMap.x && start.posMap.y == curSpend.previous.posMap.y) {
                            break;
                        }
                        if (++_count > 1000)
                        {
                            Debug.Log("·�����ȳ���1000!!!");
                        }
                        curSpend = curSpend.previous;
                    }
                    break;
                }
                openList.Remove(curSpend);
                closeList.Add(curSpend);
                // ���ھӽ���f = g + h
                byte isCorner = 0;
                foreach (Vector2SByte direction in neighbors) {
                    ++isCorner;
                    int _x = curSpend.posMap.x + direction.x;
                    int _y = curSpend.posMap.y + direction.y;
                    // �����±�
                    if (!isCanReach(new Vector3Int(_x,_y,0))) continue;
                    Spend neighbor = mapSpend[_x, _y];
                    // �رն��в�����
                    if (closeList.Contains(neighbor)) continue;
                    float temp;
                    if (isCorner > 4)
                    {
                        temp = curSpend.g + 1.414f; // б������
                    }
                    else
                    {
                        temp = curSpend.g + 1.0f; // ��������
                    }
                    // �򿪶����Ѿ����������ֵ��С��g
                    if (openList.Contains(neighbor))
                    {
                        // ����,�����ýڵ�
                        if (temp >= neighbor.g) continue;
                        neighbor.g = temp;
                    }
                    else // �����κ��б��� 
                    {
                        neighbor.g = temp;
                        openList.Add(neighbor);
                    }
                    neighbor.h = Mathf.Abs(end.posMap.x - neighbor.posMap.x) + Mathf.Abs(end.posMap.y - neighbor.posMap.y);
                    neighbor.f = neighbor.g + neighbor.h;
                    neighbor.previous = curSpend; // ����
                }
                if(count++ > 10)
                {
                    count = 0;
                    yield return null;
                }
            }
            if (path.Count == curIterCount)
            {
                Debug.Log("δ�ҵ�·��:" + start.posMap.y + ":" + start.posMap.x + "-->" + end.posMap.y + ":" + end.posMap.x);
            }
            // ToTargetLABҪע��
            IsSeeking = false;
        }

        /// <summary>
        /// ����·���ƶ�
        /// </summary>
        public bool moveByPath()
        {
            // ����·����һ��Ŀ��㣬�л���һ��Ŀ���
            if (path.Count != 0 &&
                Mathf.Abs(path[0].posMap.y - transform.position.x) < 0.01f &&
                Mathf.Abs(path[0].posMap.x - transform.position.y) < 0.01f) {
                path.RemoveAt(0); // --path.Count 
            }
            // remove�����ֹ����Խ��
            if (path.Count == 0) { 
                return true;
            }
            // ��Ϊ��ʵ����
            Vector2 forward = new Vector2(path[0].posMap.y - transform.position.x, path[0].posMap.x - transform.position.y);
            transform.Translate(forward.normalized * Time.deltaTime * moveSpeed, Space.World);//��ǰ�ƶ�
            return false;
        }

        protected override void death()
        {
        }

        /// <summary>
        /// �Ƿ���Եִ�(������������ײ���Tile,��ʹ�����ڽ����е�)
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
        // ��������
        public Vector3Int posMap;
        public float f = 0;
        public float g = 0;
        public float h = 0;
        public Spend previous; // ָ��·����ǰһ��λ��

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