using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class CutTreeTask : WorkerTask
    {
        public CutTreeTask()
        {
            BuildAvailableNeighborPos.Add(neighbors[1]);
            BuildAvailableNeighborPos.Add(neighbors[3]);
        }

        public override void finish(Worker worker)
        {
            ResourceMap.Instance.cutTree(TargetMap);
        }

        public class CutTreeTaskBuilder
        {
            private CutTreeTask task;

            public CutTreeTaskBuilder()
            {
                task = new CutTreeTask();
            }

            public CutTreeTaskBuilder setTarget(Vector3Int targetMap)
            {
                task.TargetMap = targetMap;
                return this;
            }

            public CutTreeTask build()
            {
                return task;
            }
        }
    }
}