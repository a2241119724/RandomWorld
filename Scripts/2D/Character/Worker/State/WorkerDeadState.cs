namespace LAB2D.Character.Worker.State
{
    using LAB2D;
    using Photon.Pun;

    public class WorkerDeadState : AWorkerState
    {
        public WorkerDeadState(AWorker worker)
        : base(worker)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();

            // 删除Worker定位按钮
            // LocateWorkerUI 由 UI 层直接引用，暂不抽出 Provider
            AWorkerTask.LocateWorkerUIRemoveProvider((AWorker)this.Character);

            // 释放床绑定
            AWorkerTask.FurnitureBedProvider((AWorker)this.Character);

            // 放弃当前正在执行的任务，避免任务永久被标记为"执行中"
            AWorker.WorkerData workerData = this.Character.CharacterDataLAB as AWorker.WorkerData;
            if (workerData.Task != null)
            {
                AWorker.GiveUpTaskProvider(this.Character, workerData.Task);
                workerData.Task = null;
            }

            // 删除搬运任务的预设
            AWorkerTask.InventoryProvider().DeleteWorkerPre(this.Character);

            // 丢弃拿取的东西
            this.Character.DropResource();

            AWorkerTask.NetworkDestroyProvider(this.Character.gameObject);
        }
    }
}
