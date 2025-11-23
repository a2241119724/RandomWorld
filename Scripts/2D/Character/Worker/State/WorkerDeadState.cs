namespace LAB2D
{
    public class WorkerDeadState : AWorkerState
    {
        public WorkerDeadState(AWorker worker)
        : base(worker)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            LocateWorkerUI.Instance.RemoveWorkerItem(this.Character);
            InventoryManager.Instance.DeleteWorkerPre(this.Character);
        }
    }
}
