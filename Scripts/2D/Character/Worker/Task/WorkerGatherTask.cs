using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    public class WorkerGatherTask : WorkerTask
    {
        private string resourceName = "Tree";

        public WorkerGatherTask() : base(TaskType.Gather)
        {
            stageInit.Add((Worker worker) =>
            {
                maxProgress = 10.0f;
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

        public override void finish(Worker worker)
        {
            base.finish(worker);
            ResourceMap.Instance.cutTree(TargetMap);
            List<DropItem> dropItems = DropItemManager.Instance.NameToDrop[resourceName];
            if (dropItems == null) return;
            // 采摘掉落木头,苹果
            for (int i=0;i< dropItems.Count; i++)
            {
                Vector3Int pos = IsAvailableMap.Instance.genAvailablePosMap(TargetMap,2);
                ResourceInfo resourceInfo = new ResourceInfo(ItemDataManager.Instance.getByName(dropItems[i].Name).id, 10);
                ItemMap.Instance.putDown(pos, (TileBase)ResourcesManager.Instance.getAsset(dropItems[i].Name),
                    resourceInfo, dropItems[i].ItemType);
            }
            // 删除采摘图标
            GatherMap.Instance.cancelGather(TargetMap);
        }

        public override bool isCanWork(Worker worker)
        {
            if (!base.isCanWork(worker))
            {
                return false;
            }
            return ResourceMap.Instance.ResourceMapDataLAB.TreeCurCount > 0;
        }

        public class GatherTaskBuilder
        {
            private WorkerGatherTask task;

            public GatherTaskBuilder()
            {
                task = new WorkerGatherTask();
            }

            public GatherTaskBuilder setTarget(Vector3Int targetMap)
            {
                task.TargetMap = targetMap;
                // 显示正在采摘图标
                GatherMap.Instance.addGather(targetMap);
                return this;
            }

            public GatherTaskBuilder setGatherName(string name)
            {
                task.resourceName = name;
                return this;
            }

            public WorkerGatherTask build()
            {
                return task;
            }
        }
    }
}