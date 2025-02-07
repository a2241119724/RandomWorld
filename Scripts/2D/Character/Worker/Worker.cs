using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
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
        /// <summary>
        /// �Ƿ���Ҫ������Ŀ���
        /// ����Խ��ǰ���ȼ�Խ�ߣ����ۣ�
        /// �Ƿ��������������͵Ŀ���(toogle��˳����TaskType��˳�����)
        /// Build,Carry,CutTree
        /// </summary>
        public bool[] TaskToggle { get; set; }
        public float CurTired { get; set; } = 100.0f;
        public const float MaxTired = 100.0f;
        public const float ThresholdTired = 10.0f;
        public float CurHungry { get; set; } = 100.0f;
        public const float MaxHungry = 100.0f;
        public const float ThresholdHungry = 10.0f;
        public int MaxResourceCount { get; set; } = 30;
        public LineRenderer LineRenderer { get; set; }
        public WearData WearData;
        public BedItem BedItem;

        private static Spend[,] mapSpend; // ��ͼ�а��Ļ���
        private List<Spend> openList;
        private List<Spend> closeList;
        private List<Spend> path; // Ѱ··��
        private Coroutine coroutine; // Ѱ··��
        private CheckBug checkBug;
        private static readonly List<Vector2SByte> neighbors = new List<Vector2SByte>(){
            new Vector2SByte(0,1), // ��
            new Vector2SByte(1,0), // ��
            new Vector2SByte(0,-1), // ��
            new Vector2SByte(-1,0), // ��
            new Vector2SByte(1,1), // ����
            new Vector2SByte(1,-1), // ����
            new Vector2SByte(-1,-1), // ����
            new Vector2SByte(-1,1), // ����
        }; // A*ʹ�������ھ�
        private Slider progress;
        private Text nameUI;
        /// <summary>
        /// Я���Ľ�����Դ
        /// </summary>
        public NeedResource resourceInfos;
        private CharacterStatusUI statusBar; // ��¼ʵ����Ѫ��

        protected override void Awake()
        {
            base.Awake();
            checkBug = new CheckBug();
            openList = new List<Spend>();
            closeList = new List<Spend>();
            path = new List<Spend>();
            Manager = new WorkerStateManager<ICharacterState, WorkerStateType, Worker>(this);
            CharacterDataLAB.MaxHp = CharacterDataLAB.Hp = 100;
            nameUI = transform.Find("Name").GetComponent<Text>();
            WorkerState = transform.Find("State").GetComponent<Text>();
            progress = transform.Find("Progress").GetComponent<Slider>();
            progress.gameObject.SetActive(false);
            // ·��
            LineRenderer = transform.GetComponent<LineRenderer>();
            LineRenderer.startWidth = 0.05f;
            LineRenderer.endWidth = 0.05f;
            Material material = new Material(Shader.Find("Unlit/Color")); 
            material.color = new Color(UnityEngine.Random.Range(0.5f, 1.0f), UnityEngine.Random.Range(0.5f, 1.0f), UnityEngine.Random.Range(0.5f, 1.0f));
            LineRenderer.material = material;
            LineRenderer.sortingLayerName = "Highest";
            //
            TaskToggle = new bool[10];
            // Ĭ�Ͽ��ԳԷ�
            TaskToggle[((int)TaskType.Hungry)] = true;
            resourceInfos = new NeedResource();
            statusBar = transform.Find("Hp").GetComponent<CharacterStatusUI>();
            if (statusBar == null)
            {
                Debug.LogError("statusBar Not Found!!!");
                return;
            }
            WearData = new WearData();
        }

        /// <summary>
        /// �ڼ�������״̬֮���ټӵ�TaskManager��
        /// </summary>
        protected override void Start()
        {
            base.Start();
            nameUI.text = name;
            statusBar.updateStatus(CharacterDataLAB.Hp, CharacterDataLAB.MaxHp);
        }

        void Update()
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
            // ִ�е�ǰ״̬�ĺ���
            Manager.CurrentState.OnUpdate();
        }

        /// <summary>
        /// ���õ�ͼ��Ϣ
        /// </summary>
        public void initMap(int height, int width)
        {
            // ��ʼ��Ѱ·����
            mapSpend = new Spend[height, width];
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    mapSpend[i, j] = new Spend(i, j);
                }
            }
        }

        public void initSeek(Vector3Int targetMap)
        {
            if(mapSpend == null)
            {
                initMap(TileMap.Instance.Height, TileMap.Instance.Width);
            }
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
            for (int i = 0; i < TileMap.Instance.Height; i++)
            {
                for (int j = 0; j < TileMap.Instance.Width; j++)
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
            Vector3Int posMap = TileMap.Instance.worldPosToMapPos(transform.position);
            Spend start = mapSpend[posMap.x, posMap.y]; // ���
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
            Vector3Int posMap = TileMap.Instance.worldPosToMapPos(transform.position);
            Spend start = mapSpend[posMap.x, posMap.y]; // ���
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
            // ����һ��ʱ���ͷ���
            float time = 0.0f;
            // ��¼һ��ʼ��path����
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
                    while (curSpend != null && curSpend.previous != null)
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
                            yield return null;
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
                        // ��������������ʱ��б�Ų�����
                        if (!isCanReach(new Vector3Int(_x, curSpend.posMap.y, 0))
                            && !isCanReach(new Vector3Int(curSpend.posMap.x, _y, 0))) continue;
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
                    time += Time.deltaTime;
                    if(time > 2.0f)
                    {
                        // ���Ѱ·����һ��ʱ���ͷ���
                        GlobalData.Lock.SeekLock.seekLock = false;
                        //Debug.Log(name + "�ͷ���========");
                        time = 0.0f;
                    }
                    yield return null;
                    // �������˳��������ȴ�
                    while(GlobalData.Lock.SeekLock.seekLock && GlobalData.Lock.SeekLock.owner != this)
                    {
                        //Debug.Log(name + "�ȴ���");
                        yield return null;
                    }
                }
            }
            if (path.Count == curIterCount)
            {
                Debug.Log("δ�ҵ�·��:" + start.posMap.y + ":" + start.posMap.x + "-->" + end.posMap.y + ":" + end.posMap.x);
            }
            // ToTargetLABҪע��
            IsSeeking = false;
            // ��ʾ·��
            updateLine();
        }

        /// <summary>
        /// ����·��UI
        /// </summary>
        private void updateLine() {
            LineRenderer.positionCount = path.Count + 1;
            LineRenderer.SetPosition(0, transform.position);
            for (int i = 0; i < path.Count; i++)
            {
                LineRenderer.SetPosition(i + 1, TileMap.Instance.mapPosToWorldPos(path[i].posMap));
            }
        }

        /// <summary>
        /// ����·���ƶ�
        /// </summary>
        public bool moveByPath()
        {
            if (path.Count == 0) return true;
            // ��Ϊ��ʵ����
            Vector3 worldPos = TileMap.Instance.mapPosToWorldPos(path[0].posMap);
            // ����·����һ��Ŀ��㣬�л���һ��Ŀ���
            if (path.Count != 0 &&
                Mathf.Abs(worldPos.x - transform.position.x) < 0.3f &&
                Mathf.Abs(worldPos.y - transform.position.y) < 0.3f) {
                path.RemoveAt(0); // --path.Count 
            }
            // remove�����ֹ����Խ��
            if (path.Count == 0) return true;
            Vector2 forward = new Vector2(worldPos.x - transform.position.x, worldPos.y - transform.position.y);
            transform.Translate(forward.normalized * Time.deltaTime * moveSpeed, Space.World);//��ǰ�ƶ�
            updateLine();
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
            if(!ResourceMap.Instance.isCanReach(posMap)) {
                return false;
            }
            if(BuildMap.Instance.BuildTileMap.GetTile(posMap) == null)
            {
                return true;
            }
            // �ſ���ͨ��
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

        public void setProgress(float value,bool enable)
        {
            progress.value = value;
            progress.gameObject.SetActive(enable);
        }

        public override string ToString()
        {
            string resources = "";
            foreach (KeyValuePair<int, ResourceInfo> resource in resourceInfos.needs)
            {
                resources += resource.Key + ":" + resource.Value.count + "\n";
            }
            return base.ToString() + 
                $"Hungry:{CurHungry}\n" +
                $"TargetMap:{TargetMap}\n" +
                resources;
        }

        public void addResource(ResourceInfo resourceInfo)
        {
            if (resourceInfo.count == 0) return;
            if (resourceInfos.needs.ContainsKey(resourceInfo.id))
            {
                resourceInfos.needs[resourceInfo.id].count += resourceInfo.count;
            }
            else
            {
                resourceInfos.needs.Add(resourceInfo.id, resourceInfo);
            }
        }

        public void subResource(NeedResource needResource) {
            foreach(KeyValuePair<int, ResourceInfo> need in needResource.needs)
            {
                if (resourceInfos.needs.ContainsKey(need.Key))
                {
                    resourceInfos.needs[need.Key].count -= need.Value.count;
                }
                else
                {
                    Debug.Log("������Դ��������Ȼ����ɹ�������");
                }
            }
        }

        public void subResource(ResourceInfo resourceInfo)
        {
            if (resourceInfo.count == 0) return;
            if (resourceInfos.needs.ContainsKey(resourceInfo.id))
            {
                resourceInfos.needs[resourceInfo.id].count -= resourceInfo.count;
            }
            else
            {
                Debug.Log("������Դ��������Ȼ����ɹ�������");
            }
        }

        /// <summary>
        /// ���Я������Դ����
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public int getResourceCountById(int id)
        {
            if (resourceInfos.needs.ContainsKey(id))
            {
                return resourceInfos.needs[id].count;
            }
            return 0;
        }

        /// <summary>
        /// �ж�workerЯ������Դ����������
        /// </summary>
        /// <returns></returns>
        public bool isEnough(NeedResource needResource)
        {
            foreach (KeyValuePair<int, ResourceInfo> need in needResource.needs)
            {
                if (!resourceInfos.needs.ContainsKey(need.Key) || resourceInfos.needs[need.Key].count < need.Value.count)
                {
                    return false;
                }
            }
            return true;
        }

        public void giveUpTask()
        {
            WorkerTaskManager.Instance.giveUpTask(Manager.Task);
            Manager.Task = null;
            Manager.changeState(WorkerStateType.Seek);
        }

        /// <summary>
        /// ��������Ҫ����Դ������ȥworker������������Դ����
        /// </summary>
        /// <param name="needResource"></param>
        /// <returns></returns>
        public NeedResource getRemaining(NeedResource needResource) {
            NeedResource remaining = new NeedResource();
            foreach (KeyValuePair<int, ResourceInfo> need in needResource.needs)
            {
                if (resourceInfos.needs.ContainsKey(need.Key))
                {
                    remaining.needs.Add(need.Key, new ResourceInfo(need.Key, need.Value.count - resourceInfos.needs[need.Key].count));
                }
                else
                {
                    remaining.needs.Add(need.Key, Tool.DeepCopyByBinary(need.Value));
                }
            }
            return remaining;
        }

        /// <summary>
        /// ��Ѫ
        /// </summary>
        /// <param name="Hp">������Ѫ��</param>
        public override void reduceHp(float Hp)
        {
            if (Hp <= 0)
            {
                Debug.LogError("Hp can't less than zero!!!");
                return;
            }
            base.reduceHp(Hp);
            statusBar.updateStatus(CharacterDataLAB.Hp, CharacterDataLAB.MaxHp);
            Manager.changeState(WorkerStateType.Attack);
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

    public class WearData
    {
        /// <summary>
        /// Я��������
        /// </summary>
        public Weapon weapon;
        /// <summary>
        /// ����Я����װ��
        /// </summary>
        public Dictionary<Equipment.EquipType, Equipment> equipments;

        public WearData()
        {
            equipments = new Dictionary<Equipment.EquipType, Equipment>();
        }
    }
}