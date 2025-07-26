namespace LAB2D
{
    /// <summary>
    /// Worker管理器
    /// </summary>
    public class WorkerManager : CharacterManager<WorkerManager, Worker, WorkerCreator>
    {
        /// <summary>
        /// 用于多个Worker概率获取寻路锁
        /// </summary>
        private int countLock = 1;

        /// <inheritdoc/>
        public override void Add(Worker character)
        {
            base.Add(character);
            WorkerInfoUI.Instance.AddWorkerItem(character);
        }

        /// <summary>
        /// 每次获取-1，到0后再变为寻路Worker的数量
        /// 用于Worker随机获取寻路锁
        /// </summary>
        /// <returns>当前寻路Worker的数量</returns>
        public int GetCountLock()
        {
            if (this.countLock == 1)
            {
                // 初始时或只有一个Worker在寻路时, 获取寻路Worker的数量
                foreach (Worker worker in this.Characters)
                {
                    if (worker.Manager.CurrentStateType == WorkerStateType.Seek)
                    {
                        this.countLock++;
                    }
                }

                return this.countLock;
            }
            else
            {
                return --this.countLock;
            }
        }
    }
}
