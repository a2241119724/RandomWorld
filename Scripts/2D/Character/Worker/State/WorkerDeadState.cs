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

            // 删除搬运任务的预设
            InventoryManager.Instance.DeleteWorkerPre(this.Character);

            // 丢弃拿取的东西
            this.Character.DropResource();

            PhotonNetwork.Destroy(this.Character.gameObject);
        }
    }
}
