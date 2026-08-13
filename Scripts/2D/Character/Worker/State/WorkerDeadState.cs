namespace LAB2D.Character.Worker.State
{
    using LAB2D;
    using Photon.Pun;
    using UnityEngine;

    public class WorkerDeadState : AWorkerState
    {
        public WorkerDeadState(AWorker worker)
        : base(worker)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();

            // 死亡诊断（事件点）：记录 Worker 死亡位置，供复盘死亡原因与地理位置。
            Vector3Int deadPos = AWorkerTask.TileMapWorldToMapProvider(this.Character.transform.position);
            AWorkerTask.LogProvider(
                $"[StateDiag] {this.Character.name} Worker死亡 pos=({deadPos.x},{deadPos.y})",
                LogManager.LogLevelEnum.Warning);

            // 删除Worker定位按钮
            // LocateWorkerUI 由 UI 层直接引用，暂不抽出 Provider
            Core.GameServices.LocateWorkerUIRemoveProvider((AWorker)this.Character);

            // 释放床绑定
            Core.GameServices.FurnitureBedProvider((AWorker)this.Character);

            // 放弃当前正在执行的任务，避免任务永久被标记为"执行中"
            AWorker.WorkerData workerData = this.Character.CharacterDataLAB as AWorker.WorkerData;
            if (workerData.Task != null)
            {
                AWorker.GiveUpTaskProvider(this.Character, workerData.Task);
                workerData.Task = null;
            }

            // 删除任务管理器中所有与该Worker相关的任务（悬赏发布、专属任务等）
            Core.ServiceLocator.Get<WorkerTaskManager>().RemoveTasksForWorker(this.Character.GetInstanceID());

            // 删除搬运任务的预设
            AWorkerTask.InventoryProvider().DeleteWorkerPre(this.Character);

            // 丢弃拿取的东西
            this.Character.DropResource();

            Core.GameServices.NetworkDestroyProvider(this.Character.gameObject);
        }
    }
}
