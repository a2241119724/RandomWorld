namespace LAB2D.Item
{
    using LAB2D;
    using LAB2D.Character.Worker;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 农作物管理
    /// </summary>
    public class FarmlandManager : Singleton<FarmlandManager>
    {
        private readonly Dictionary<int, Dictionary<Vector3Int, PlantInfo>> id2Resource; // 同一个id对应的所有位置
        private readonly Dictionary<Vector3Int, PlantInfo> cells;
        private readonly Dictionary<AWorker, Dictionary<Vector3Int, PlantInfo>> preGatherResource; // 预采集资源
        private readonly Dictionary<AWorker, Dictionary<Vector3Int, PlantInfo>> prePlantResource; // 预种植资源

        public FarmlandManager()
        {
            this.cells = new Dictionary<Vector3Int, PlantInfo>();
            this.id2Resource = new Dictionary<int, Dictionary<Vector3Int, PlantInfo>>();
            this.preGatherResource = new Dictionary<AWorker, Dictionary<Vector3Int, PlantInfo>>();
            this.prePlantResource = new Dictionary<AWorker, Dictionary<Vector3Int, PlantInfo>>();
        }

        /// <summary>
        /// 添加整个建筑物的所有墙
        /// </summary>
        /// <param name="startPos">起始位置</param>
        /// <param name="width">宽度</param>
        /// <param name="length">高度</param>
        public void AddCells(Vector3Int startPos, int width = 10, int length = 7)
        {
            if (!this.id2Resource.ContainsKey(-1))
            {
                this.id2Resource.Add(-1, new Dictionary<Vector3Int, PlantInfo>());
            }

            Dictionary<Vector3Int, PlantInfo> resources = this.id2Resource[-1];
            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    Vector3Int pos = VectorTool.Add(startPos, i, j);
                    this.cells.Add(pos, new PlantInfo(-1, 0, 0));
                    resources.Add(pos, new PlantInfo(-1, 0, 0));
                }
            }
        }

        /// <summary>
        /// 获取没有植物的土壤位置
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <param name="resourceInfo">资源信息</param>
        /// <param name="isPre">是否需要预建造</param>
        /// <returns>位置</returns>
        public Vector3Int IsEnoughAndPrePlant(AWorker worker, ResourceInfo resourceInfo, bool isPre = false)
        {
            if (!this.id2Resource.ContainsKey(-1) || this.id2Resource[-1].Count == 0)
            {
                return default;
            }

            Dictionary<Vector3Int, PlantInfo>.KeyCollection.Enumerator enumerator = this.id2Resource[-1].Keys.GetEnumerator();
            enumerator.MoveNext();
            if (isPre)
            {
                if (!this.prePlantResource.ContainsKey(worker))
                {
                    this.prePlantResource.Add(worker, new Dictionary<Vector3Int, PlantInfo>());
                }

                this.prePlantResource[worker].Add(enumerator.Current, new PlantInfo(resourceInfo.Id, 1, 0));
            }

            return enumerator.Current;
        }

        /// <summary>
        /// 根据预种植进行种植
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <param name="posMap">位置</param>
        public void PlantByPrePlant(AWorker worker, Vector3Int posMap)
        {
            if (this.prePlantResource.ContainsKey(worker) && this.prePlantResource[worker].ContainsKey(posMap))
            {
                PlantInfo plantInfo = this.prePlantResource[worker][posMap];
                if (!this.id2Resource.ContainsKey(plantInfo.Id))
                {
                    this.id2Resource.Add(plantInfo.Id, new Dictionary<Vector3Int, PlantInfo>());
                }

                this.id2Resource[plantInfo.Id].Add(posMap, plantInfo);
                this.id2Resource[-1].Remove(posMap);
                this.cells[posMap] = plantInfo;
                this.prePlantResource[worker].Remove(posMap);
                ResourceMap.Instance.SetTile(posMap, ResourceManager.Instance.GetAsset(
                    ItemDataManager.Instance.GetById(plantInfo.Id).EnName));
            }
        }

        /// <summary>
        /// 预采集
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <param name="plantInfo">农作物信息</param>
        /// <param name="isPre">是否预采集</param>
        /// <returns>位置</returns>
        public Vector3Int PreGather(AWorker worker, PlantInfo plantInfo, bool isPre = false)
        {
            if (this.id2Resource[plantInfo.Id].Count == 0)
            {
                return default;
            }

            Dictionary<Vector3Int, PlantInfo>.KeyCollection.Enumerator enumerator = this.id2Resource[plantInfo.Id].Keys.GetEnumerator();
            enumerator.MoveNext();
            if (!this.preGatherResource.ContainsKey(worker))
            {
                this.preGatherResource.Add(worker, new Dictionary<Vector3Int, PlantInfo>());
            }

            this.preGatherResource[worker].Add(enumerator.Current, plantInfo);
            return enumerator.Current;
        }

        /// <summary>
        /// 根据预采集进行采集
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <param name="posMap">位置</param>
        public void GatherByPreGather(AWorker worker, Vector3Int posMap)
        {
            if (this.preGatherResource.ContainsKey(worker) && this.preGatherResource[worker].ContainsKey(posMap))
            {
                PlantInfo plantInfo = this.preGatherResource[worker][posMap];
                this.id2Resource[plantInfo.Id].Remove(posMap);
                this.id2Resource[-1].Add(posMap, plantInfo);
                this.cells[posMap] = plantInfo;
                this.preGatherResource[worker].Remove(posMap);
            }
        }

        /// <summary>
        /// 农作物信息
        /// </summary>
        public class PlantInfo
        {
            /// <summary>
            /// ID
            /// </summary>
            public int Id;

            /// <summary>
            /// 数量
            /// </summary>
            public int Count;

            /// <summary>
            /// 生长时间
            /// </summary>
            public int Time;

            public PlantInfo(int id, int count, int time)
            {
                this.Id = id;
                this.Count = count;
                this.Time = time;
            }
        }
    }
}
