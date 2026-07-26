namespace LAB2D.Character
{
    using LAB2D;
    using UnityEngine;

    /// <summary>
    /// 角色创建器
    /// </summary>
    /// <typeparam name="T">实际的创建器</typeparam>
    public abstract class CharacterCreator<T> : Singleton<T>, ICharacterCreator
        where T : new()
    {
        /// <inheritdoc/>
        public virtual GameObject Create(Vector3 worldPos = default)
        {
            if (worldPos == default)
            {
                if (!Core.ServiceLocator.TryGet(out TileMap tm))
                {
                    AWorkerTask.LogProvider("TileMap.Instance is null, cannot create character at valid position", LogManager.LogLevelEnum.Error);
                    return null;
                }

                worldPos = AWorkerTask.TileMapPositionProvider(AWorkerTask.GenCanReachPosProvider(default));
            }

            return this.DoCreate(worldPos, string.Empty, string.Empty);
        }

        /// <summary>
        /// 实际创建
        /// </summary>
        /// <param name="worldPos">位置</param>
        /// <param name="name">名称</param>
        /// <param name="layer">层级</param>
        /// <returns>对象</returns>
        protected virtual GameObject DoCreate(Vector3 worldPos, string name, string layer)
        {
            // 计算世界坐标偏移
            Vector3 spawnPos = worldPos;
            if (Core.ServiceLocator.TryGet(out TileMap tm) && tm.gameObject != null)
            {
                spawnPos = new Vector3(
                    worldPos.x + tm.gameObject.transform.position.x,
                    worldPos.y + tm.gameObject.transform.position.y,
                    tm.gameObject.transform.position.z);
            }
            else
            {
                AWorkerTask.LogProvider("TileMap.Instance is null, creating character at raw world position", LogManager.LogLevelEnum.Warning);
            }

            // 设置角色
            GameObject g = Core.ServiceLocator.Get<ResourceManager>().Instantiate(
                name,
                spawnPos,
                Quaternion.identity,
                false);
            if (g == null)
            {
                return null;
            }

            // 设置层级
            g.layer = LayerMask.NameToLayer(layer);
            return g;
        }
    }
}
