namespace LAB2D.Item
{
    using LAB2D;
    using LAB2D.Character.Worker;
    using LAB2D.Data;
    using LAB2D.Serializable;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 家具管理（ASingletonSaveData）。
    /// </summary>
    public class FurnitureManager : ASingletonSaveData<FurnitureManager>
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

                worker.BedItem = new SingleBed();

                // 同步家位置到 WorkerData（持久化 + O(1) 查找）
                var wd = worker.CharacterDataLAB as AWorker.WorkerData;
                if (wd != null) wd.HomePosition = Vector3IntLAB.ToVector3IntLAB(posMap);
            }
            else
            {
                this.BedToWorker[posMap].BedItem = null;
                this.BedToWorker[posMap] = worker;

                worker.BedItem = new SingleBed();

                // 同步家位置到 WorkerData
                var wd = worker.CharacterDataLAB as AWorker.WorkerData;
                if (wd != null) wd.HomePosition = Vector3IntLAB.ToVector3IntLAB(posMap);
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

                // 清除 WorkerData 中的家位置
                var wd = worker.CharacterDataLAB as AWorker.WorkerData;
                if (wd != null) wd.HomePosition = null;
            }
        }

        /// <summary>
        /// 获取一个无主床（遗弃房间）的位置，供新 Worker 继承。
        /// </summary>
        /// <returns>第一个空床的位置，没有则返回 null</returns>
        public Vector3Int? GetAbandonedBedPosition()
        {
            foreach (KeyValuePair<Vector3Int, AWorker> kv in this.BedToWorker)
            {
                if (kv.Value == null)
                {
                    return kv.Key;
                }
            }

            return null;
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

        /// <inheritdoc/>
        public override void SaveData()
        {
            base.SaveData();
            FurnitureManagerData data = new FurnitureManagerData();

            foreach (KeyValuePair<Vector3Int, AWorker> kv in this.BedToWorker)
            {
                data.Beds.Add(new BedEntry
                {
                    PosX = kv.Key.x,
                    PosY = kv.Key.y,
                    PosZ = kv.Key.z,
                    // 保存绑定的 Worker 名称（用于读档后重建绑定）
                    WorkerName = kv.Value != null ? kv.Value.name : string.Empty,
                });
            }

            DataTool.SaveDataByBinary(GlobalData.ConfigFile.GetPath(this.GetType().Name), data);
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            base.LoadData();
            FurnitureManagerData data = DataTool.LoadDataByBinary<FurnitureManagerData>(GlobalData.ConfigFile.GetPath(this.GetType().Name));
            if (data == null || data.Beds == null)
            {
                return;
            }

            foreach (BedEntry entry in data.Beds)
            {
                Vector3Int pos = new Vector3Int(entry.PosX, entry.PosY, entry.PosZ);
                this.AddBed(pos);

                // 尝试重建 Worker 绑定
                if (!string.IsNullOrEmpty(entry.WorkerName))
                {
                    AWorker worker = this.FindWorkerByName(entry.WorkerName);
                    if (worker != null)
                    {
                        this.BedToWorker[pos] = worker;
                        worker.BedItem = new SingleBed();
                    }
                }
            }
        }

        /// <summary>
        /// 通过名称查找 Worker（用于读档恢复绑定）。
        /// </summary>
        private AWorker FindWorkerByName(string workerName)
        {
            List<AWorker> workers = Core.ServiceLocator.Get<WorkerManager>().Characters;
            foreach (AWorker w in workers)
            {
                if (w != null && w.name == workerName)
                {
                    return w;
                }
            }

            return null;
        }

        [Serializable]
        public class FurnitureManagerData
        {
            public List<BedEntry> Beds = new List<BedEntry>();
        }

        [Serializable]
        public class BedEntry
        {
            public int PosX;
            public int PosY;
            public int PosZ;
            public string WorkerName;
        }
    }
}
