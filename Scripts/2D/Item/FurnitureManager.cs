namespace LAB2D.Item
{
    using LAB2D;
    using LAB2D.Character.Worker;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 家具管理
    /// </summary>
    public class FurnitureManager : Singleton<FurnitureManager>
    {
        /// <summary>
        /// Worker与床绑定
        /// </summary>
        public Dictionary<Vector3Int, AWorker> BedToWorker;

        public FurnitureManager()
        {
            this.BedToWorker = new Dictionary<Vector3Int, AWorker>();
        }

        /// <summary>
        /// 添加床
        /// </summary>
        /// <param name="posMap">位置</param>
        public void AddBed(Vector3Int posMap)
        {
            if (this.BedToWorker.ContainsKey(posMap))
            {
                return;
            }

            this.BedToWorker.Add(posMap, null);
        }

        /// <summary>
        /// 添加床预Worker的绑定关系
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <param name="worker">Worker</param>
        public void AddWorkerToBed(Vector3Int posMap, AWorker worker)
        {
            if (!this.BedToWorker.ContainsKey(posMap))
            {
                return;
            }

            if (this.BedToWorker[posMap] == null)
            {
                this.BedToWorker[posMap] = worker;

                // TODO
                worker.BedItem = new SingleBed();
            }
            else
            {
                this.BedToWorker[posMap].BedItem = null;
                this.BedToWorker[posMap] = worker;

                // TODO
                worker.BedItem = new SingleBed();
            }
        }

        /// <summary>
        /// 释放Worker的床绑定
        /// </summary>
        /// <param name="worker">Worker</param>
        public void RemoveWorkerFromBed(AWorker worker)
        {
            Vector3Int key = default;
            bool found = false;
            foreach (KeyValuePair<Vector3Int, AWorker> kv in this.BedToWorker)
            {
                if (kv.Value == worker)
                {
                    key = kv.Key;
                    found = true;
                    break;
                }
            }

            if (found)
            {
                this.BedToWorker[key] = null;
                worker.BedItem = null;
            }
        }

        /// <summary>
        /// 通过床获取Worker
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <returns>Worker</returns>
        public AWorker GetWorkerByBed(Vector3Int posMap)
        {
            if (!this.BedToWorker.ContainsKey(posMap))
            {
                return null;
            }

            return this.BedToWorker[posMap];
        }
    }
}
