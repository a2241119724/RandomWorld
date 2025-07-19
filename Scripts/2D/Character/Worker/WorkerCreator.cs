namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// Worker创建器
    /// </summary>
    public class WorkerCreator : CharacterCreator<WorkerCreator>
    {
        /// <inheritdoc/>
        protected override GameObject _create(Vector3 worldPos, string name, string layer)
        {
            GameObject g = base._create(worldPos, "Worker", "Worker");
            g.name = NameGenertor.Instance.GetRandomName();
            return g;
        }
    }
}
