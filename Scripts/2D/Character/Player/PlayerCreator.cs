using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class PlayerCreator : CharacterCreator<PlayerCreator>
    {
        /// <summary>
        /// 实例化玩家
        /// </summary>
        protected override GameObject _create()
        {
            Vector3 posMap = TileMap.Instance.genAvailablePosMap();
            //设置角色
            GameObject g = PhotonNetwork.Instantiate(ResourcesManager.Instance.getPath("Player.prefab"), 
                new Vector3(posMap.y + TileMap.Instance.gameObject.transform.position.x,
                posMap.x + TileMap.Instance.gameObject.transform.position.y, 
                TileMap.Instance.gameObject.transform.position.z), 
                Quaternion.identity);
            if (g == null)
            {
                Debug.LogError("player Instantiate Error!!!");
                return null;
            }
            // 设置层级
            g.layer = LayerMask.NameToLayer("Player");
            return g;
        }
    }
}