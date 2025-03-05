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
        /// �Ƿ��������������͵Ŀ���(toogle��˳����TaskType��˳�����)
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
        public static Lock seekLock = new Lock();
        
        /// <summary>
        /// Я������Դ
        /// </summary>
        private Dictionary<int, ResourceInfo> resourceInfos;
        private static Spend[,] mapSpend; // ��ͼ�а��Ļ���
        private List<Spend> openList;
        private List<Spend> closeList;
        private List<Spend> path; // Ѱ··��
        private Coroutine coroutine; // Ѱ··��
        private static readonly List<Vector2SByte> neighbors = new List<Vector2SByte>(){
            new Vector2SByte(0,1), new Vector2SByte(1,0), new Vector2SByte(0,-1), new Vector2SByte(-1,0), // ��������
            //new Vector2SByte(1,1), new Vector2SByte(1,-1), // ����,����
            //new Vector2SByte(-1,-1), new Vector2SByte(-1,1), // ����, ����
        }; // A*ʹ�������ھ�
        private Slider progress;
        private Text nameUI;
        private CharacterStatusUI statusBar; // ��¼ʵ����Ѫ��

        protected override void Awake()
        {
            base.Awake();
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
            TaskToggle[(int)TaskType.Hungry] = true;
            TaskToggle[(int)TaskType.Wear] = true;
            TaskToggle[(int)TaskType.Carry] = true;
            resourceInfos = new Dictionary<int, ResourceInfo>();
            statusBar = transform.Find("Hp").GetComponent<CharacterStatusUI>();
            if (statusBar == null)
            {
                LogManager.Instance.log("statusBar Not Found!!!", LogManager.LogLevel.Error);
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
                initMap(TileMap.Height, TileMap.Width);
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
            updateLine();
            SeekProgress = 0.0f;
            for (int i = 0; i < TileMap.Height; i++)
            {
                for (int j = 0; j < TileMap.Width; j++)
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
            if (!TileMap.Instance.isFreeTile(targetMap))
            {
                LogManager.Instance.log("�����߽�!!!", LogManager.LogLevel.Error);
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
            List<Spend> _path = new List<Spend>();
            float totalDistance = Mathf.Sqrt(Mathf.Pow(start.posMap.x - end.posMap.x, 2) 
                + Mathf.Pow(start.posMap.y - end.posMap.y, 2));
            openList.Add(start);
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
                SeekProgress = Mathf.Sqrt(Mathf.Pow(curSpend.posMap.x - start.posMap.x, 2) 
                    + Mathf.Pow(curSpend.posMap.y - start.posMap.y, 2)) / totalDistance;
                // �ж��Ƿ񵽴��յ�(�˴�ֻ��������)
                if ((int)curSpend.posMap.x == (int)end.posMap.x && (int)curSpend.posMap.y == (int)end.posMap.y)
                {
                    //LogManager.Instance.log("�ҵ�·��!!!", LogManager.LogLevel.Info);
                    // ��·��
                    Vector3Int lastDet = new Vector3Int(0,0);
                    Spend _curSpend = curSpend;
                    while (curSpend != null && curSpend.previous != null)
                    {
                        //// �Ż�(һ��ֱ��ֻ����ֹ�ڵ�)
                        //if (curSpend.previous.posMap.x - curSpend.posMap.x != lastDet.x || curSpend.previous.posMap.y - curSpend.posMap.y != lastDet.y)
                        //{
                        //    //LogManager.Instance.log("����" + curSpend.posMap.y + " " + curSpend.posMap.x, LogManager.LogLevel.Info);
                        //    _path.Insert(curIterCount, curSpend);
                        //    lastDet.x = curSpend.previous.posMap.x - curSpend.posMap.x;
                        //    lastDet.y = curSpend.previous.posMap.y - curSpend.posMap.y;
                        //}
                        _path.Insert(curIterCount, curSpend);
                        // ���ܳ���ѭ��·��
                        if (_curSpend != null)
                        {
                            _curSpend = _curSpend.previous;
                            if(_curSpend != null)
                            {
                                _curSpend = _curSpend.previous;
                            }
                        }
                        if (_curSpend != null && _curSpend.posMap.x == curSpend.previous.posMap.x 
                            && _curSpend.posMap.y == curSpend.previous.posMap.y) {
                            LogManager.Instance.log("WorkerѰ·���ֻ�·", LogManager.LogLevel.Error);
                            break;
                        }
                        if (FrameControl.Instance.isNeedStop())
                        {
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
                if (FrameControl.Instance.isNeedStop())
                {
                    time += Time.deltaTime;
                    if(time > 1.0f)
                    {
                        // ���Ѱ·����һ��ʱ���ͷ���
                        seekLock.releaseLock(this);
                        time = 0.0f;
                    }
                    yield return null;
                    // �������˳��������ȴ�
                    yield return new WaitUntil(() => seekLock.getLock(this));
                }
            }
            // �Ż������߼��
            //if (_path.Count > 1)
            //{
            //    start = _path[0];
            //    path.Add(start);
            //    for (int i = 2; i < _path.Count; i++)
            //    {
            //        RaycastHit2D hit = Physics2D.Raycast(TileMap.Instance.mapPosToWorldPos(start.posMap),
            //            TileMap.Instance.mapPosToWorldPos(_path[i].posMap) - TileMap.Instance.mapPosToWorldPos(start.posMap),
            //            Vector3.Distance(TileMap.Instance.mapPosToWorldPos(start.posMap), TileMap.Instance.mapPosToWorldPos(_path[i].posMap)));
            //        if (hit.collider == null) continue;
            //        // ��ǰ�ڵ㲻��ֱ��ֱ���ߣ�������һ���ڵ�
            //        path.Add(_path[i - 1]);
            //        start = _path[i - 1];
            //    }
            //    path.Add(_path[_path.Count - 1]);
            //}
            if (_path.Count > 1)
            {
                int lastIndex = 0;
                while (lastIndex < _path.Count - 1)
                {
                    bool isUpdate = false;
                    int _count = 0;
                    start = _path[lastIndex];
                    path.Add(start);
                    for (int i = lastIndex+1; i < _path.Count; i++)
                    {
                        if (_count > 50) break;
                        if (FrameControl.Instance.isNeedStop())
                        {
                            yield return null;
                        }
                        // ����ƽ��һ������
                        Vector3 pos = TileMap.Instance.mapPosToWorldPos(start.posMap);
                        Vector3 direction = TileMap.Instance.mapPosToWorldPos(_path[i].posMap) - TileMap.Instance.mapPosToWorldPos(start.posMap);
                        float distance = Vector3.Distance(TileMap.Instance.mapPosToWorldPos(start.posMap), TileMap.Instance.mapPosToWorldPos(_path[i].posMap));
                        RaycastHit2D hit = Physics2D.Raycast(new Vector2(pos.x - 0.5f, pos.y),direction, distance);
                        if(hit.collider == null)
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
                path.Add(_path[_path.Count - 1]);
            }
            if (path.Count == curIterCount)
            {
                LogManager.Instance.log(name + "δ�ҵ�·��:" + start.posMap.y + ":" + start.posMap.x 
                    + "-->" + end.posMap.y + ":" + end.posMap.x, LogManager.LogLevel.Error);
            }
            // ��ʾ·��
            updateLine();
            // ToTargetLABҪע��
            IsSeeking = false;
            //seekLock.releaseLock(this);
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
                Mathf.Abs(worldPos.x - transform.position.x) < 0.2f &&
                Mathf.Abs(worldPos.y - transform.position.y) < 0.2f) {
                path.RemoveAt(0); // --path.Count 
            }
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
            if (!TileMap.Instance.isCanReach(posMap)) {
                return false;
            }
            if(!ResourceMap.Instance.isCanReach(posMap)) {
                return false;
            }
            if(!BuildMap.Instance.isCanReach(posMap))
            {
                return false;
            }
            return true;
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            checkBug.addColliderCount(DateTime.Now.Ticks);
            if (checkBug.isBug(name, 100))
            {
                Manager.changeState(WorkerStateType.Seek);
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
            foreach (KeyValuePair<int, ResourceInfo> resource in resourceInfos)
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
            if (resourceInfos.ContainsKey(resourceInfo.id))
            {
                resourceInfos[resourceInfo.id].count += resourceInfo.count;
            }
            else
            {
                resourceInfos.Add(resourceInfo.id, Tool.DeepCopyByBinary(resourceInfo));
            }
        }

        public void subResource(Dictionary<int, ResourceInfo> needResource) {
            foreach(KeyValuePair<int, ResourceInfo> need in needResource)
            {
                if (resourceInfos.ContainsKey(need.Key))
                {
                    resourceInfos[need.Key].count -= need.Value.count;
                }
                else
                {
                    LogManager.Instance.log("������Դ��������Ȼ����ɹ�������", LogManager.LogLevel.Error);
                }
            }
        }

        public void subResource(ResourceInfo resourceInfo)
        {
            if (resourceInfo.count == 0) return;
            if (resourceInfos.ContainsKey(resourceInfo.id))
            {
                resourceInfos[resourceInfo.id].count -= resourceInfo.count;
            }
            else
            {
                LogManager.Instance.log("������Դ��������Ȼ����ɹ�������", LogManager.LogLevel.Error);
            }
        }

        /// <summary>
        /// ���Я������Դ����
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public int getResourceCountById(int id)
        {
            if (resourceInfos.ContainsKey(id))
            {
                return resourceInfos[id].count;
            }
            return 0;
        }

        /// <summary>
        /// �ж�workerЯ������Դ����������
        /// </summary>
        /// <returns></returns>
        public bool isEnough(Dictionary<int, ResourceInfo> needResource)
        {
            foreach (KeyValuePair<int, ResourceInfo> need in needResource)
            {
                if (!resourceInfos.ContainsKey(need.Key) || resourceInfos[need.Key].count < need.Value.count)
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
        public Dictionary<int, ResourceInfo> getRemaining(Dictionary<int, ResourceInfo> needResource) {
            Dictionary<int, ResourceInfo> remaining = new Dictionary<int, ResourceInfo>();
            foreach (KeyValuePair<int, ResourceInfo> need in needResource)
            {
                if (resourceInfos.ContainsKey(need.Key))
                {
                    remaining.Add(need.Key, new ResourceInfo(need.Key, need.Value.count - resourceInfos[need.Key].count));
                }
                else
                {
                    remaining.Add(need.Key, Tool.DeepCopyByBinary(need.Value));
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
                LogManager.Instance.log("Hp can't less than zero!!!", LogManager.LogLevel.Error);
                return;
            }
            base.reduceHp(Hp);
            statusBar.updateStatus(CharacterDataLAB.Hp, CharacterDataLAB.MaxHp);
            Manager.changeState(WorkerStateType.Attack);
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

        public void addEquipment(Equipment equipment, Vector3Int posMap)
        {
            if (equipments.ContainsKey(equipment.equipType))
            {
                // ����װ��
                Equipment _equipment = equipments[equipment.equipType];
                ItemMap.Instance.putDownToInventory(posMap, ResourcesManager.Instance.getAsset(equipment.ToString()),
                    new ResourceInfo(equipment.id, 1));
                equipments[equipment.equipType] = equipment;
            }
            else
            {
                equipments.Add(equipment.equipType, equipment);
            }
        }
    }
}