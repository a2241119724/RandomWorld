using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    public class CutTreeTask : WorkerTask
    {
        private readonly string[] dropItems = new string[] { "Wood", "Apple" };
        private readonly ItemType[] dropItemTypes = new ItemType[] { ItemType.Material, ItemType.Food };

        public CutTreeTask()
        {
            Name = TaskType.CutTree.ToString();
            TaskType = TaskType.CutTree;
            stageInit.Add((Worker worker) =>
            {
                maxProgress = 2000;
                AvailableNeighborPos.Clear();
                AvailableNeighborPos.Add(neighbors[1]);
                AvailableNeighborPos.Add(neighbors[3]);
                // 进入工作状态
                worker.Manager.changeState(WorkerStateType.Seek);
            });
        }

        public override void start(Worker worker)
        {
            base.start(worker);
            stageInit[0].Invoke(worker);
        }

        protected override void finish(Worker worker)
        {
            ResourceMap.Instance.cutTree(TargetMap);
            // 砍树掉落木头,苹果
            for(int i=0;i< dropItems.Length; i++)
            {
                Vector3Int pos = IsAvailableMap.Instance.genAvailablePosMap(TargetMap,2);
                ResourceMap.Instance.putDown(pos, (TileBase)ResourcesManager.Instance.getAsset(dropItems[i]));
                ResourceInfo resourceInfo = new ResourceInfo(ItemDataManager.Instance.getByName(dropItems[i]).id, 10);
                // 添加到掉落物管理中
                DropResourceManager.Instance.addDrop(dropItemTypes[i], pos, resourceInfo);
                // 添加搬运任务
                WorkerTaskManager.Instance.addTask(new CarryTask.CarryTaskBuilder()
                    //.setEndTarget(InventoryManager.Instance.getCell(id))
                    .setResourceInfo(Tool.DeepCopyByBinary(resourceInfo)).setStartTarget(pos).build());
            }
        }

        public override bool isCanWork(Worker worker)
        {
            if (!base.isCanWork(worker))
            {
                return false;
            }
            return ResourceMap.Instance.ResourceMapDataLAB.TreeCurCount > 0;
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