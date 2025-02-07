using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LAB2D
{
    public class InventoryManager : Singleton<InventoryManager>
    {
        /// <summary>
        /// ͬһ�����Ͷ�Ӧ������λ��
        /// </summary>
        public Dictionary<ItemType, Dictionary<Vector3Int, ResourceInfo>> TypeToResource { get; set; }

        /// <summary>
        /// ����pos����Դ
        /// </summary>
        private Dictionary<Vector3Int, ResourceInfo> posToResource;
        /// <summary>
        /// ͬһ��id��Ӧ������λ��
        /// </summary>
        private Dictionary<int, Dictionary<Vector3Int, ResourceInfo>> idToResource;
        /// <summary>
        /// Ԥ������Դ
        /// </summary>
        private Dictionary<Worker, Dictionary<Vector3Int, ResourceInfo>> preTakeResource;
        /// <summary>
        /// Ԥ���õ���Դ
        /// </summary>
        private Dictionary<Worker, Dictionary<Vector3Int, ResourceInfo>> prePlaceResource;
        /// <summary>
        /// ����cell������
        /// </summary>
        private int capacity = 1000;

        public InventoryManager() {
            posToResource = new Dictionary<Vector3Int, ResourceInfo>();
            idToResource = new Dictionary<int, Dictionary<Vector3Int, ResourceInfo>>();
            preTakeResource = new Dictionary<Worker, Dictionary<Vector3Int, ResourceInfo>>();
            prePlaceResource = new Dictionary<Worker, Dictionary<Vector3Int, ResourceInfo>>();
            TypeToResource = new Dictionary<ItemType, Dictionary<Vector3Int, ResourceInfo>>();
        }

        /// <summary>
        /// �½��ֿ�ʱ������cell
        /// posToResource,idToResource,typeToResource�е�ResourceInfo����
        /// </summary>
        /// <param name="startPos"></param>
        /// <param name="width"></param>
        /// <param name="length"></param>
        public void addCells(Vector3Int startPos, int width = 10, int length = 7)
        {
            Dictionary<Vector3Int, ResourceInfo> idTo = new Dictionary<Vector3Int, ResourceInfo>();
            Dictionary<Vector3Int, ResourceInfo> typeTo = new Dictionary<Vector3Int, ResourceInfo>();
            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    Vector3Int pos = Tool.add(startPos, i, j);
                    ResourceInfo resourceInfo = new ResourceInfo(-1, 0);
                    posToResource.Add(pos, resourceInfo);
                    idTo.Add(pos, resourceInfo);
                    typeTo.Add(pos, resourceInfo);
                }
            }
            // ����ֵ���Ҫ����
            idToResource.Add(-1, idTo);
            TypeToResource.Add(ItemType.Null, typeTo);
        }

        /// <summary>
        /// �õ�һ��Ԥ���õ�λ��
        /// </summary>
        /// <param name="worker"></param>
        /// <returns></returns>
        public Vector3Int getPosByPrePlace(Worker worker)
        {
            if (prePlaceResource.ContainsKey(worker))
            {
                return prePlaceResource[worker].First().Key;
            }
            Debug.Log("û��Ԥ������Դ");
            return default;
        }

        /// <summary>
        /// ����㹻���ã���ôԤ������Դ
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="resourceInfo"></param>
        /// <returns></returns>
        public bool isEnoughAndPrePlace(Worker worker, ResourceInfo resourceInfo, bool isPre = false)
        {
            Dictionary<Vector3Int, ResourceInfo> pre = new Dictionary<Vector3Int, ResourceInfo>();
            int remaining = resourceInfo.count;
            // ���ֿ��д��ڸ�id,��Ӧλ�õ���Դ�������λ��Ԥ������Դ������֮���Ƿ񳬹�����
            if (idToResource.ContainsKey(resourceInfo.id))
            {
                foreach (KeyValuePair<Vector3Int, ResourceInfo> cell in idToResource[resourceInfo.id])
                {
                    // ���Է��ã�����Ҫ�ж�preTake,��Ϊʵ���ϻ�û������
                    if (cell.Value.count + getPrePlaceCountByPos(cell.Key) < capacity)
                    {
                        int count = capacity - (cell.Value.count + getPrePlaceCountByPos(cell.Key));
                        // ��������
                        if (remaining <= count)
                        {
                            if (isPre)
                            {
                                pre.Add(cell.Key, new ResourceInfo(resourceInfo.id, remaining));
                                foreach (KeyValuePair<Vector3Int, ResourceInfo> pair in pre)
                                {
                                    prePlace(worker, pair.Key, pair.Value);
                                }
                            }
                            return true;
                        }
                        // û�з�����
                        else
                        {
                            if (isPre)
                            {
                                pre.Add(cell.Key, new ResourceInfo(resourceInfo.id, count));
                            }
                            remaining -= count;
                        }
                    }
                }
                // ��id��Ӧ������cell���˲��ܷ�����Դ����ҪѰ�ҿյ�cell
            }
            // �ֿ���û�ж�Ӧid��cell,��ҪѰ�ҿյ�cell
            if (!idToResource.ContainsKey(-1))
            {
                Debug.Log("�ֿ�����,����");
                return false;
            }
            // �ҵ�û��Ԥ���õ�λ��
            foreach (KeyValuePair<Vector3Int, ResourceInfo> cell in idToResource[-1])
            {
                // ��λ��û�б�Ԥ����
                if (!isAreadyPrePlace(cell.Key, resourceInfo.id))
                {
                    int count = capacity - (cell.Value.count + getPrePlaceCountByPos(cell.Key));
                    // ��������
                    if (remaining <= count)
                    {
                        if (isPre)
                        {
                            pre.Add(cell.Key, new ResourceInfo(resourceInfo.id, remaining));
                            foreach (KeyValuePair<Vector3Int, ResourceInfo> pair in pre)
                            {
                                prePlace(worker, pair.Key, pair.Value);
                            }
                        }
                        return true;
                    }
                    // û�з�����
                    else
                    {
                        if (isPre)
                        {
                            pre.Add(cell.Key, new ResourceInfo(resourceInfo.id, count));
                        }
                        remaining -= count;
                    }
                }
            }
            // �п��ܱ�Ԥ������
            Debug.Log("�ֿ�����");
            return false;
        }

        /// <summary>
        /// ��λ���Ƿ�������id�Ѿ�Ԥ����
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool isAreadyPrePlace(Vector3Int pos, int id)
        {
            foreach (KeyValuePair<Worker, Dictionary<Vector3Int, ResourceInfo>> prePlace in prePlaceResource)
            {
                // ������id�Ѿ�Ԥ�����ˣ�����һ��Cell
                if (prePlace.Value.ContainsKey(pos) && prePlace.Value[pos].id != id)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// ͨ��id��ȡԤ������Դ������
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private int getPrePlaceCountById(int id) {
            int count = 0;
            foreach (KeyValuePair<Worker, Dictionary<Vector3Int, ResourceInfo>> prePlace in prePlaceResource) {
                foreach (KeyValuePair<Vector3Int, ResourceInfo> pre in prePlace.Value)
                {
                    if (pre.Value.id == id)
                    {
                        count += pre.Value.count;
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// ͨ��pos��ȡԤ������Դ������
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private int getPrePlaceCountByPos(Vector3Int pos)
        {
            int count = 0;
            foreach (KeyValuePair<Worker, Dictionary<Vector3Int, ResourceInfo>> prePlace in prePlaceResource)
            {
                if (prePlace.Value.ContainsKey(pos))
                {
                    count += prePlace.Value.Count;
                }
            }
            return count;
        }

        /// <summary>
        /// Ԥ������Դ�������ܲ����ڳ�������֮ǰ����
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="pos"></param>
        /// <param name="resourceInfo"></param>
        private void prePlace(Worker worker, Vector3Int pos, ResourceInfo resourceInfo)
        {
            if (prePlaceResource.ContainsKey(worker))
            {
                if (prePlaceResource[worker].ContainsKey(pos))
                {
                    prePlaceResource[worker][pos].count += resourceInfo.count;
                    ItemInfoUI.Instance.updateInfo(this.GetType().Name, pos, ToString(pos));
                    return;
                }
                prePlaceResource[worker].Add(pos, Tool.DeepCopyByBinary(resourceInfo));
                ItemInfoUI.Instance.updateInfo(this.GetType().Name, pos, ToString(pos));
                return;
            }
            Dictionary<Vector3Int, ResourceInfo> dict = new Dictionary<Vector3Int, ResourceInfo>();
            dict.Add(pos, Tool.DeepCopyByBinary(resourceInfo));
            prePlaceResource.Add(worker, dict);
            ItemInfoUI.Instance.updateInfo(this.GetType().Name, pos, ToString(pos));
        }

        /// <summary>
        /// �жϲֿ����Ƿ��Ӧ���͵���Ʒ����Ԥ������Դ
        /// TODOû��Ԥȡ
        /// </summary>
        /// <returns></returns>
        public bool isEnoughFoodAndPreTake(Worker worker, float hungry, bool isPre = false) {
            Dictionary<Vector3Int, ResourceInfo> foods = new Dictionary<Vector3Int, ResourceInfo>();
            foreach (KeyValuePair<Vector3Int, ResourceInfo> food in TypeToResource[ItemType.Food])
            {
                float _hungry = food.Value.count * 10.0f;
                // �㹻�Ա�
                if (_hungry >= hungry)
                {
                    if (isPre)
                    {
                        foods.Add(food.Key, new ResourceInfo(food.Value.id, (int)(hungry / 10.0f)));
                        foreach (KeyValuePair<Vector3Int, ResourceInfo> pair in foods)
                        {
                            preTake(worker, pair.Key, pair.Value);
                        }
                    }
                    return true;
                }
                // ��ǰid�Բ���
                else
                {
                    hungry -= _hungry;
                    if (isPre)
                    {
                        foods.Add(food.Key, Tool.DeepCopyByBinary(food.Value));
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Ԥȡ��Դ,û�п��ǳ������������Է�װΪisEnough
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="pos"></param>
        /// <param name="resourceInfo"></param>
        private void preTake(Worker worker, Vector3Int pos, ResourceInfo resourceInfo)
        {
            if (!preTakeResource.ContainsKey(worker))
            {
                Dictionary<Vector3Int, ResourceInfo> dict = new Dictionary<Vector3Int, ResourceInfo>();
                dict.Add(pos, Tool.DeepCopyByBinary(resourceInfo));
                preTakeResource.Add(worker, dict);
                ItemInfoUI.Instance.updateInfo(this.GetType().Name, pos, ToString(pos));
                return;
            }
            if (!preTakeResource[worker].ContainsKey(pos))
            {
                preTakeResource[worker].Add(pos, Tool.DeepCopyByBinary(resourceInfo));
                ItemInfoUI.Instance.updateInfo(this.GetType().Name, pos, ToString(pos));
                return;
            }
            preTakeResource[worker][pos].count += resourceInfo.count;
            ItemInfoUI.Instance.updateInfo(this.GetType().Name, pos, ToString(pos));
        }

        /// <summary>
        /// ͨ��Ԥ�������
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="pos"></param>
        /// <param name="resourceInfo"></param>
        public ResourceInfo addItemByPrePlace(Worker worker, Vector3Int posMap)
        {
            if (!prePlaceResource[worker].ContainsKey(posMap))
            {
                Debug.Log("û��Ԥ������Դ������");
                return null;
            }
            ResourceInfo resourceInfo = prePlaceResource[worker][posMap];
            // ɾ��Ԥ���õ���Դ
            prePlaceResource[worker].Remove(posMap);
            // ��ӵ��ֿ�����������
            // ��Ȼ�Ѿ�Ԥ�����ˣ���һ�����Է��ã����ᳬ������
            if (posToResource[posMap].id == -1)
            {
                transferResource(posMap, -1, resourceInfo.id, ItemType.Null, ItemDataManager.Instance.getTypeById(resourceInfo.id));
            }
            posToResource[posMap].id = resourceInfo.id;
            posToResource[posMap].count += resourceInfo.count;
            return resourceInfo;
        }

        /// <summary>
        /// ��ȡһ��Ԥ����Դ��λ��
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public Vector3Int getPosByPreTake(Worker worker) {
            if (preTakeResource.ContainsKey(worker))
            {
                return preTakeResource[worker].First().Key;
            }
            Debug.Log("û��Ԥ����Դ");
            return default;
        }

        private int getPreTakeCountByPos(Vector3Int pos)
        {
            int count = 0;
            foreach (KeyValuePair<Worker, Dictionary<Vector3Int, ResourceInfo>> preTake in preTakeResource)
            {
                if (preTake.Value.ContainsKey(pos))
                {
                    count += preTake.Value[pos].count;
                }
            }
            DebugUI.Instance.updateTaskInfo(pos + " " + count);
            return count;
        }

        private int getPreTakeCountById(int id)
        {
            int count = 0;
            foreach (KeyValuePair<Worker, Dictionary<Vector3Int, ResourceInfo>> prePlace in preTakeResource)
            {
                foreach (KeyValuePair<Vector3Int, ResourceInfo> pre in prePlace.Value)
                {
                    if (pre.Value.id == id)
                    {
                        count += pre.Value.count;
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// ����Ԥȡ����Դɾ���ֿ��еĿ��
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="posMap"></param>
        /// <returns>���شӲֿ��пۼ�������(Ԥȡ����Դ)</returns>
        public ResourceInfo subItemByPreTake(Worker worker, Vector3Int posMap) {
            if (!preTakeResource[worker].ContainsKey(posMap))
            {
                Debug.Log("û��Ԥȡ��Դ������");
                return null;
            }
            ResourceInfo resourceInfo = preTakeResource[worker][posMap];
            // ɾ��Ԥȡ����Դ
            preTakeResource[worker].Remove(posMap);
            // ���ٲֿ�����������
            posToResource[posMap].count -= resourceInfo.count;
            // �������ȡ��
            if (posToResource[posMap].count == 0)
            {
                transferResource(posMap, posToResource[posMap].id, -1, ItemDataManager.Instance.getTypeById(posToResource[posMap].id), ItemType.Null);
                ItemMap.Instance.pickUp(posMap);
                // ʳ�ﱻ����ɾ������
                if (ItemDataManager.Instance.getTypeById(posToResource[posMap].id) == ItemType.Food)
                {
                    WorkerTaskManager.Instance.deleteHungryTask(posMap);
                }
                posToResource[posMap].id = -1;
            }
            // ��������Ȼ���Ѿ�Ԥȡ�ˣ���˵���϶��ǹ���
            return resourceInfo;
        }

        /// <summary>
        /// ����idToResource,typeToResource
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="oldId"></param>
        /// <param name="newId"></param>
        /// <param name="oldType"></param>
        /// <param name="newType"></param>
        private void transferResource(Vector3Int pos, int oldId, int newId, ItemType oldType, ItemType newType) {
            // idToResource
            idToResource[oldId].Remove(pos);
            if (idToResource.ContainsKey(newId))
            {
                idToResource[newId].Add(pos, posToResource[pos]);
            }
            else
            {
                Dictionary<Vector3Int, ResourceInfo> dict = new Dictionary<Vector3Int, ResourceInfo>();
                dict.Add(pos, posToResource[pos]);
                idToResource.Add(newId, dict);
            }
            // typeToResource
            TypeToResource[oldType].Remove(pos);
            if (TypeToResource.ContainsKey(newType))
            {
                TypeToResource[newType].Add(pos, posToResource[pos]);
            }
            else
            {
                Dictionary<Vector3Int, ResourceInfo> dict = new Dictionary<Vector3Int, ResourceInfo>();
                dict.Add(pos, posToResource[pos]);
                TypeToResource.Add(newType, dict);
            }
        }

        /// <summary>
        /// ���Ƿ��㹻�����㹻��Ԥ������Դ������worker��Я�������ԴԤȡ
        /// </summary>
        /// <param name="needResource"></param>
        /// <param name="maxValue">���������Դ������</param>
        /// <returns></returns>
        public bool isEnoughAndPreTake(Worker worker, NeedResource needResource, bool isPre = false)
        {
            foreach (KeyValuePair<int, ResourceInfo> need in needResource.needs)
            {
                if (idToResource.ContainsKey(need.Key))
                {
                    int count = 0;
                    foreach (KeyValuePair<Vector3Int, ResourceInfo> resource in idToResource[need.Key])
                    {
                        count += resource.Value.count;
                    }
                    // id��Ӧ����������ȥԤȡ����Դ������С������������������
                    if (count - getPreTakeCountById(need.Key) < need.Value.count)
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            // Ԥ������Դ
            if (isPre)
            {
                foreach (KeyValuePair<int, ResourceInfo> need in needResource.needs)
                {
                    // ÿ��CellԤȡ��֮��ʣ��Cell��Ԥȡ������,����ȡneed.Value.count
                    int remaining = Mathf.Max(need.Value.count, worker.MaxResourceCount);
                    // ����WorkerЯ�������ֵԤȡ,����������ֵ��ȡ��������Դ
                    foreach (KeyValuePair<Vector3Int, ResourceInfo> resource in idToResource[need.Key])
                    {
                        int count = resource.Value.count - getPreTakeCountByPos(resource.Key);
                        if (count < remaining)
                        {
                            remaining -= count;
                            preTake(worker, resource.Key, new ResourceInfo(need.Key, count));
                        }
                        else
                        {
                            // ��ǰidȡ���ˣ�����Ҫ��ȡ��
                            preTake(worker, resource.Key, new ResourceInfo(need.Key, remaining));
                            break;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// ���ڲֿ��е����������װ��ʱ����ʾ��Ҫ������Worker�б�
        /// </summary>
        /// <param name="pos"></param>
        public void showWearMenu(Vector3Int pos)
        {
            ItemType itemType = ItemDataManager.Instance.getTypeById(posToResource[pos].id);
            if (itemType == ItemType.Weapon || itemType == ItemType.Equipment) { 
                WearTaskUI.Instance.showWearTask(pos);
            }
        }

        public string ToString(Vector3Int pos)
        {
            if (!posToResource.ContainsKey(pos)) return "";
            ResourceInfo resourceInfo = posToResource[pos];
            string text = $"id:{resourceInfo.id}\n" +
                $"count:{resourceInfo.count}\n" +
                $"prePlace:\n";
            foreach (KeyValuePair<Worker, Dictionary<Vector3Int, ResourceInfo>> prePlace in prePlaceResource)
            {
                if (prePlace.Value.ContainsKey(pos))
                {
                    text += prePlace.Key.name + ":\n"
                        + "    " + prePlace.Value[pos].id + " " + prePlace.Value[pos].count + "\n";
                }
            }
            text += "preTake:\n";
            foreach (KeyValuePair<Worker, Dictionary<Vector3Int, ResourceInfo>> preTake in preTakeResource)
            {
                if (preTake.Value.ContainsKey(pos))
                {
                    text += preTake.Key.name + ":\n"
                        + "    " + preTake.Value[pos].id + " " + preTake.Value[pos].count + "\n";
                }
            }
            return text;
        }

        public ResourceInfo getByPos(Vector3Int posMap)
        {
            if (posToResource.ContainsKey(posMap))
            {
                return posToResource[posMap];
            }
            return null;
        }
    }
}
