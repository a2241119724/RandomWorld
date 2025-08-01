namespace LAB2D
{
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
                worldPos = TileMap.Instance.MapPosToWorldPos(TileMap.Instance.GenCanReachPos());
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
            // 设置角色
            GameObject g = Tool.Instantiate(
                ResourceManager.Instance.GetPrefab(name),
                new Vector3(
                    worldPos.x + TileMap.Instance.gameObject.transform.position.x,
                    worldPos.y + TileMap.Instance.gameObject.transform.position.y,
                    TileMap.Instance.gameObject.transform.position.z),
                Quaternion.identity);
            if (g == null)
            {
                LogManager.Instance.Log(name + " Instantiate Error!!!", LogManager.LogLevel.Error);
                return null;
            }

            // 设置层级
            g.layer = LayerMask.NameToLayer(layer);
            return g;
        }
    }
}
