using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class PlayerCreator : CharacterCreator<PlayerCreator>
    {
        /// <summary>
        /// ʵ�������
        /// </summary>
        protected override GameObject _create()
        {
            Vector3 posMap = TileMap.Instance.genAvailablePosMap();
            //���ý�ɫ
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
            // ���ò㼶
            g.layer = LayerMask.NameToLayer("Player");
            return g;
        }
    }
}