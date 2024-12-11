using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    public class BuildTask : WorkerTask
    {
        private int i = 0;

        public override bool execute(Worker worker)
        {
            if (i++ > 10) {
                i = 0;
                // 将建造完成的Tile从BuildingMap放到BuildMap中
                BuildMap.Instance.setComplete(TargetMap);
                complete();
                return true;
            }
            return false;
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

            public BuildTask build() {
                return task;
            }
        }
    }
}

