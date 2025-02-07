using LAB2D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D {
    public class FurnitureManager : Singleton<FurnitureManager>
    {
        public Dictionary<Vector3Int, Worker> bedToWorker;

        public FurnitureManager()
        {
            bedToWorker = new Dictionary<Vector3Int, Worker>();
        }

        public void addBed(Vector3Int posMap) {
            if (bedToWorker.ContainsKey(posMap)) return;
            bedToWorker.Add(posMap, null);
        }

        public void addWorkerToBed(Vector3Int posMap, Worker worker) {
            if (!bedToWorker.ContainsKey(posMap)) return;
            if(bedToWorker[posMap] == null)
            {
                bedToWorker[posMap] = worker;
                // TODO
                worker.BedItem = new SingleBed();
            }
            else
            {
                bedToWorker[posMap].BedItem = null;
                bedToWorker[posMap] = worker;
                // TODO
                worker.BedItem = new SingleBed();
            }
        }

        public Worker getByBed(Vector3Int posMap)
        {
            if (!bedToWorker.ContainsKey(posMap)) return null;
            return bedToWorker[posMap];
        }
    }
}
