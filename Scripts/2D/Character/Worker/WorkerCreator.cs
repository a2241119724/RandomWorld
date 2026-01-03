namespace LAB2D
{
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
            g.name = NameGenerator.Instance.GetRandomName();
            return g;
        }
    }
}
