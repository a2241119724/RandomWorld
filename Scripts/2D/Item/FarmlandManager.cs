using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class FarmlandManager : Singleton<FarmlandManager>
    {
        private Dictionary<Vector3Int, ResourceInfo> cells;
        /// <summary>
        /// id,count
        /// </summary>
        private Dictionary<int, int> resources;
        private const int capacity = 1000;

        public FarmlandManager() {
            cells = new Dictionary<Vector3Int, ResourceInfo>();
            resources = new Dictionary<int, int>();
        }

        public void addCells(Vector3Int startPos, int width = 10, int length = 7)
        {
            for(int i = 0; i < length; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    Vector3Int pos = Tool.add(startPos, i, j);
                    cells.Add(pos,new ResourceInfo(-1, 0));
                }
            }
        }

        /// <summary>
        /// 若已经有了可以放在相同的位置
        /// 没有则返回一个Free Cell
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Vector3Int getCell(int id) {
            Vector3Int freeCell = default;
            foreach (KeyValuePair<Vector3Int, ResourceInfo> inventoryCell in cells)
            {
                if (inventoryCell.Value.id == id && inventoryCell.Value.count < capacity)
                {
                    // 直接占位,放置重复申请
                    inventoryCell.Value.id = id;
                    return inventoryCell.Key;
                }
                else if(inventoryCell.Value.id == -1 && freeCell == default)
                {
                    freeCell = inventoryCell.Key;
                }
            }
            if(freeCell != default)
            {
                cells[freeCell].id = id; // 直接占位
            }
            return freeCell;
        }

        public bool isHaveFood() {
            foreach (KeyValuePair<int, int> resource in resources)
            {
                ItemType itemType = ItemDataManager.Instance.getTypeById(resource.Key);
                if(itemType == ItemType.Food)
                {
                    return true;
                }
            }
            return false;
        }

        public void addItem(Vector3Int pos,ResourceInfo resourceInfo) { 
            if(cells[pos].id != -1 && cells[pos].id != resourceInfo.id)
            {
                Debug.Log("不能放置");
                return;
            }
            cells[pos].id = resourceInfo.id;
            cells[pos].count += resourceInfo.count;
            if (resources.ContainsKey(resourceInfo.id))
            {
                resources[resourceInfo.id] += resourceInfo.count;
            }
            else
            {
                resources.Add(resourceInfo.id, resourceInfo.count);
            }
        }

        public ResourceInfo getItem(Vector3Int posMap) {
            if (!cells.ContainsKey(posMap)) return null;
            return cells[posMap];
        }

        /// <summary>
        /// TODO可优化
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Vector3Int getPosById(int id)
        {
            foreach (KeyValuePair<Vector3Int, ResourceInfo> inventoryCell in cells)
            {
                if (inventoryCell.Value.id == id)
                {
                    return inventoryCell.Key;
                }
            }
            return default;
        }

        /// <summary>
        /// 取材料
        /// </summary>
        /// <param name="posMap"></param>
        /// <param name="resourceInfo"></param>
        /// <returns>剩余没有取到的材料数量</returns>
        public ResourceInfo subItem(Vector3Int posMap,ResourceInfo resourceInfo) {
            // 取不到
            if (cells[posMap].id == -1) return resourceInfo;
            // 够了
            if (cells[posMap].count > resourceInfo.count)
            {
                cells[posMap].count -= resourceInfo.count;
                resources[cells[posMap].id] -= resourceInfo.count;
                resourceInfo.count = 0;
                return resourceInfo;
            }
            // 正好
            else if(cells[posMap].count == resourceInfo.count)
            {
                resources[cells[posMap].id] -= resourceInfo.count;
                cells[posMap].count = 0;
                resourceInfo.count = 0;
                ItemMap.Instance.pickUp(posMap);
                // 食物被吃完删除任务
                if (ItemDataManager.Instance.getTypeById(cells[posMap].id) == ItemType.Food)
                {
                    WorkerTaskManager.Instance.deleteHungryTask(posMap);
                }
                cells[posMap].id = -1;
                return resourceInfo;
            }
            // 不够
            else
            {
                resources[cells[posMap].id] -= cells[posMap].count;
                resourceInfo.count -= cells[posMap].count;
                cells[posMap].count = 0;
                ItemMap.Instance.pickUp(posMap);
                // 食物被吃完删除任务
                if (ItemDataManager.Instance.getTypeById(cells[posMap].id) == ItemType.Food)
                {
                    WorkerTaskManager.Instance.deleteHungryTask(posMap);
                }
                cells[posMap].id = -1;
                return resourceInfo;
            }
        }

        public bool isEnough(NeedResource needResource)
        {
            foreach (KeyValuePair<int,ResourceInfo> need in needResource.needs)
            {
                if(!resources.ContainsKey(need.Key) || resources[need.Key] < need.Value.count)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="posMap"></param>
        /// <param name="hungry"></param>
        /// <returns>获得增加的饥饿值</returns>
        public float subFood(Vector3Int posMap, float hungry) {
            int needCount = Mathf.RoundToInt(hungry / 10);
            ResourceInfo resourceInfo = subItem(posMap,new ResourceInfo(-1, needCount));
            return (needCount - resourceInfo.count) * 10.0f;
        }
    }
}
