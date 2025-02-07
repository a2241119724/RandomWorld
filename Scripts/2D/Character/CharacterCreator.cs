using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D {
    public abstract class CharacterCreator<T> : Singleton<T>,ICharacterCreator where T : new()
    {
        protected virtual GameObject _create(Vector3 worldPos, string name, string layer) {
            //设置角色
            GameObject g = Tool.Instantiate(ResourcesManager.Instance.getPrefab(name),
                new Vector3(worldPos.x + TileMap.Instance.gameObject.transform.position.x,
                worldPos.y + TileMap.Instance.gameObject.transform.position.y,
                TileMap.Instance.gameObject.transform.position.z),
                Quaternion.identity);
            if (g == null)
            {
                Debug.LogError(name + " Instantiate Error!!!");
                return null;
            }
            // 设置层级
            g.layer = LayerMask.NameToLayer(layer);
            return g;
        }

        public virtual GameObject create(Vector3 worldPos = default)
        {
            if(worldPos == default)
            {
                worldPos = TileMap.Instance.mapPosToWorldPos(TileMap.Instance.genAvailablePosMap());
            }
            return _create(worldPos,"","");
        }
    }

    public interface ICharacterCreator
    {
        GameObject create(Vector3 worldPos = default);
    }
}
