using UnityEngine;

namespace LAB2D
{
    public abstract class CharacterCreator<T> : Singleton<T>, ICharacterCreator where T : new()
    {
        protected virtual GameObject _create(Vector3 worldPos, string name, string layer)
        {
            //设置角色
            GameObject g = Tool.Instantiate(ResourcesManager.Instance.GetPrefab(name),
                new Vector3(worldPos.x + TileMap.Instance.gameObject.transform.position.x,
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

        public virtual GameObject create(Vector3 worldPos = default)
        {
            if (worldPos == default)
            {
                worldPos = TileMap.Instance.MapPosToWorldPos(TileMap.Instance.GenCanReachPos());
            }
            return _create(worldPos, "", "");
        }
    }

    public interface ICharacterCreator
    {
        GameObject create(Vector3 worldPos = default);
    }
}
