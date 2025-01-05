using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    public class BuildTask : WorkerTask
    {
        /// <summary>
        /// 没用
        /// </summary>
        public BuildItem BuildItem { get; private set; }

        public BuildTask() {
            BuildAvailableNeighborPos.Add(neighbors[0]);
            BuildAvailableNeighborPos.Add(neighbors[1]);
            BuildAvailableNeighborPos.Add(neighbors[2]);
            BuildAvailableNeighborPos.Add(neighbors[3]);
        }

        public override void finish(Worker worker)
        {
            // 将建造完成的Tile从BuildingMap放到BuildMap中
            BuildMap.Instance.setComplete(TargetMap);
        }

        public class BuildTaskBuilder {
            private BuildTask task;

            public BuildTaskBuilder(){
                task = new BuildTask();
            }

            public BuildTaskBuilder setTarget(Vector3Int targetMap) {
                task.TargetMap = targetMap;
                return this;
            }

            public BuildTaskBuilder setBuild(BuildItem buildItem)
            {
                task.BuildItem = buildItem;
                return this;
            }

            public BuildTask build() {
                return task;
            }
        }
    }
}

