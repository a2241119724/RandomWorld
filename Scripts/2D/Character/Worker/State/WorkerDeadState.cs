namespace LAB2D
{
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
            LocateWorkerUI.Instance.RemoveWorkerItem(this.Character);

            // 释放床绑定
            FurnitureManager.Instance.RemoveWorkerFromBed(this.Character);

            // 放弃当前正在执行的任务，避免任务永久被标记为"执行中"
            AWorker.WorkerData workerData = this.Character.CharacterDataLAB as AWorker.WorkerData;
            if (workerData.Task != null)
            {
                WorkerTaskManager.Instance.GiveUpTask(workerData.Task);
                workerData.Task = null;
            }

            // 删除搬运任务的预设
            InventoryManager.Instance.DeleteWorkerPre(this.Character);

            // 丢弃拿取的东西
            this.Character.DropResource();

            PhotonNetwork.Destroy(this.Character.gameObject);
        }
    }
}
