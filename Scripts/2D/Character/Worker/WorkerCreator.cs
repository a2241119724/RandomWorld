namespace LAB2D.Character.Worker
{
    using LAB2D;
    using UnityEngine;

    /// <summary>
    /// Worker创建器
    /// </summary>
    public class WorkerCreator : CharacterCreator<WorkerCreator>
    {
        /// <inheritdoc/>
        protected override GameObject DoCreate(Vector3 worldPos, string name, string layer)
        {
            GameObject g = base.DoCreate(worldPos, PrefabConstant.WORKER, LayerConstant.WORKER_LAYER);
            string workerName = Core.GameServices.NameGeneratorProvider();
            g.name = workerName;

            // 持久化名字到 CharacterData，确保读档后 BuildMap.BuilderName 能匹配
            AWorker worker = g.GetComponent<AWorker>();
            if (worker?.CharacterDataLAB != null)
            {
                worker.CharacterDataLAB.Name = workerName;
            }

            return g;
        }
    }
}
