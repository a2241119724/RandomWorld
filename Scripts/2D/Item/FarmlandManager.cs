using ExitGames.Client.Photon.StructWrapping;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class FarmlandManager : Singleton<FarmlandManager>
    {
        private Dictionary<Vector3Int, PlantInfo> cells;
        /// <summary>
        /// 同一个id对应的所有位置
        /// </summary>
        private Dictionary<int, Dictionary<Vector3Int, PlantInfo>> idToResource;
        /// <summary>
        /// 预采集资源
        /// </summary>
        private Dictionary<Worker, Dictionary<Vector3Int, PlantInfo>> preGatherResource;
        /// <summary>
        /// 预种植资源
        /// </summary>
        private Dictionary<Worker, Dictionary<Vector3Int, PlantInfo>> prePlantResource;

        public FarmlandManager()
        {
            cells = new Dictionary<Vector3Int, PlantInfo>();
            idToResource = new Dictionary<int, Dictionary<Vector3Int, PlantInfo>>();
            preGatherResource = new Dictionary<Worker, Dictionary<Vector3Int, PlantInfo>>();
            prePlantResource = new Dictionary<Worker, Dictionary<Vector3Int, PlantInfo>>();
        }

        public void addCells(Vector3Int startPos, int width = 10, int length = 7)
        {
            if (!idToResource.ContainsKey(-1))
            {
                idToResource.Add(-1, new Dictionary<Vector3Int, PlantInfo>());
            }
            Dictionary<Vector3Int, PlantInfo> idTo = idToResource[-1];
            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    Vector3Int pos = Tool.add(startPos, i, j);
                    cells.Add(pos, new PlantInfo(-1, 0, 0));
                    idTo.Add(pos, new PlantInfo(-1, 0, 0));
                }
            }
        }

        /// <summary>
        /// 获取没有植物的土壤位置
        /// </summary>
        public Vector3Int isEnoughAndPrePlant(Worker worker, ResourceInfo resourceInfo, bool isPre = false)
        {
            if (!idToResource.ContainsKey(-1) || idToResource[-1].Count == 0) return default;
            Dictionary<Vector3Int, PlantInfo>.KeyCollection.Enumerator enumerator = idToResource[-1].Keys.GetEnumerator();
            enumerator.MoveNext();
            if (isPre)
            {
                if (!prePlantResource.ContainsKey(worker))
                {
                    prePlantResource.Add(worker, new Dictionary<Vector3Int, PlantInfo>());
                }
                prePlantResource[worker].Add(enumerator.Current, new PlantInfo(resourceInfo.id, 1, 0));
            }
            return enumerator.Current;
        }

        public void plantByPrePlant(Worker worker, Vector3Int posMap)
        {
            if (prePlantResource.ContainsKey(worker) && prePlantResource[worker].ContainsKey(posMap))
            {
                PlantInfo plantInfo = prePlantResource[worker][posMap];
                if (!idToResource.ContainsKey(plantInfo.id))
                {
                    idToResource.Add(plantInfo.id, new Dictionary<Vector3Int, PlantInfo>());
                }
                idToResource[plantInfo.id].Add(posMap, plantInfo);
                idToResource[-1].Remove(posMap);
                cells[posMap] = plantInfo;
                prePlantResource[worker].Remove(posMap);
                ResourceMap.Instance.setTile(posMap, ResourcesManager.Instance.getAsset(
                    ItemDataManager.Instance.getById(plantInfo.id).imageName));
            }
        }

        public Vector3Int preGather(Worker worker, PlantInfo plantInfo, bool isPre = false)
        {
            if (idToResource[plantInfo.id].Count == 0) return default;
            Dictionary<Vector3Int, PlantInfo>.KeyCollection.Enumerator enumerator = idToResource[plantInfo.id].Keys.GetEnumerator();
            enumerator.MoveNext();
            if (!preGatherResource.ContainsKey(worker))
            {
                preGatherResource.Add(worker, new Dictionary<Vector3Int, PlantInfo>());
            }
            preGatherResource[worker].Add(enumerator.Current, plantInfo);
            return enumerator.Current;
        }

        public void gatherByPreGather(Worker worker, Vector3Int posMap)
        {
            if (preGatherResource.ContainsKey(worker) && preGatherResource[worker].ContainsKey(posMap))
            {
                PlantInfo plantInfo = preGatherResource[worker][posMap];
                idToResource[plantInfo.id].Remove(posMap);
                idToResource[-1].Add(posMap, plantInfo);
                cells[posMap] = plantInfo;
                preGatherResource[worker].Remove(posMap);
            }
        }

        public class PlantInfo
        {
            public int id;
            public int count;
            public int time;

            public PlantInfo(int id, int count, int time)
            {
                this.id = id;
                this.count = count;
                this.time = time;
            }
        }
    }
}
